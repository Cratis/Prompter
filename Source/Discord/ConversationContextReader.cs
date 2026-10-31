// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Discord;

/// <summary>Builds bounded request-only context after admission, degrading safely to current-only.</summary>
/// <param name="history">The public, same-scope history source.</param>
/// <param name="time">The clock enforcing the history age limit.</param>
public sealed class ConversationContextReader(IConversationHistory history, TimeProvider time)
{
    /// <summary>The separate hard ceiling for the latest request.</summary>
    public const int MaximumCurrentCharacters = 4000;
    const int MaximumMessages = 10;
    const int MaximumHistoryCharacters = 6000;

    /// <summary>Builds context without fetching anything unless a spend lease is active.</summary>
    /// <param name="current">The explicit latest request.</param>
    /// <param name="location">Only the triggering public channel or thread.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The sanitized latest question with optional untrusted history.</returns>
    /// <exception cref="WorkRefused">The latest request exceeds its separate hard ceiling.</exception>
    public Task<Question> For(string current, ConversationLocation location, CancellationToken cancellationToken = default) =>
        Read(current, location, null, cancellationToken);

    /// <summary>Builds context like the plain overload, also keeping the Prompter message the request replies to.</summary>
    /// <param name="current">The explicit latest request.</param>
    /// <param name="location">Only the triggering public channel or thread.</param>
    /// <param name="anchor">The message being replied to; used only when it is an earlier Prompter message in the same channel.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The sanitized latest question with optional untrusted history.</returns>
    /// <exception cref="WorkRefused">The latest request exceeds its separate hard ceiling.</exception>
    public Task<Question> For(string current, ConversationLocation location, HistoryMessage anchor, CancellationToken cancellationToken = default) =>
        Read(current, location, anchor, cancellationToken);

    static Question Degraded(Question question) => question with { Conversation = new([], IsDegraded: true) };

    static bool EligibleAnchor(HistoryMessage? anchor, ConversationLocation location) =>
        anchor is not null && anchor.ChannelId == location.ChannelId && anchor.Id < location.MessageId && anchor.IsSelf && !anchor.IsSystem;

    static string Clip(HistoryMessage message, int remaining)
    {
        var content = ConversationText.Sanitize(message.Content);
        return content.Length > remaining ? content[..remaining] : content;
    }

    static string AnchorText(HistoryMessage? anchor, ConversationLocation location)
    {
        if (!EligibleAnchor(anchor, location)) return string.Empty;
        var text = ConversationText.Sanitize(anchor!.Content);
        return text.Length > MaximumHistoryCharacters ? text[..MaximumHistoryCharacters] : text;
    }

    async Task<Question> Read(string current, ConversationLocation location, HistoryMessage? anchor, CancellationToken cancellationToken)
    {
        if (current.Length > MaximumCurrentCharacters) throw new WorkRefused();
        var question = new Question(ConversationText.Sanitize(current));
        try
        {
            var lease = WorkLease.Current;
            if (lease?.Token.IsCancellationRequested != false || cancellationToken.IsCancellationRequested) return Degraded(question);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1), time);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.Token, timeout.Token);
            if (!await history.IsPublic(location, deadline.Token).WaitAsync(deadline.Token)) return Degraded(question);
            var messages = await history.Before(location, deadline.Token).WaitAsync(deadline.Token);
            return question with { Conversation = new(Bound(messages, location, anchor)) };
        }
        catch (Exception)
        {
            // History is optional. Do not log message text, identities, or transport errors.
            return Degraded(question);
        }
    }

    ReadOnlyCollection<ConversationMessage> Bound(IReadOnlyList<HistoryMessage> messages, ConversationLocation location, HistoryMessage? anchor)
    {
        var anchored = AnchorText(anchor, location);
        ulong? anchoredId = anchored.Length > 0 ? anchor!.Id : null;
        var selected = new List<(ulong Id, ConversationMessage Message)>();
        if (anchoredId is { } id) selected.Add((id, new("assistant", anchored)));
        var remaining = MaximumHistoryCharacters - anchored.Length;
        foreach (var message in messages.OrderByDescending(message => message.Id).Take(MaximumMessages).Where(message => Eligible(message, location) && message.Id != anchoredId))
        {
            var content = Clip(message, remaining);
            if (content.Length == 0) continue;
            selected.Add((message.Id, new(message.IsSelf ? "assistant" : "user", content)));
            remaining -= content.Length;
        }

        return selected.OrderBy(entry => entry.Id).Select(entry => entry.Message).ToList().AsReadOnly();
    }

    bool Eligible(HistoryMessage message, ConversationLocation location) =>
        message.ChannelId == location.ChannelId && message.Id < location.MessageId &&
        message.CreatedAt < location.CreatedAt && message.CreatedAt >= time.GetUtcNow().AddMinutes(-15) &&
        !message.IsSystem && (!message.IsBot || message.IsSelf);
}
