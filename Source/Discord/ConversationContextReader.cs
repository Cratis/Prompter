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
    public async Task<Question> For(string current, ConversationLocation location, CancellationToken cancellationToken = default)
    {
        if (current.Length > MaximumCurrentCharacters) throw new WorkRefused();
        var question = new Question(ConversationText.Sanitize(current));
        try
        {
            var lease = WorkLease.Current;
            if (lease?.Token.IsCancellationRequested != false || cancellationToken.IsCancellationRequested) return Degraded(question);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(1));
            if (!await history.IsPublic(location, deadline.Token).WaitAsync(deadline.Token)) return Degraded(question);
            var messages = await history.Before(location, deadline.Token).WaitAsync(deadline.Token);
            return question with { Conversation = new(Bound(messages, location)) };
        }
        catch (Exception)
        {
            // History is optional. Do not log message text, identities, or transport errors.
            return Degraded(question);
        }
    }

    static Question Degraded(Question question) => question with { Conversation = new([], IsDegraded: true) };

    ReadOnlyCollection<ConversationMessage> Bound(IReadOnlyList<HistoryMessage> messages, ConversationLocation location)
    {
        var remaining = MaximumHistoryCharacters;
        var selected = new List<ConversationMessage>();
        foreach (var message in messages.OrderByDescending(message => message.Id).Take(MaximumMessages).Where(message => Eligible(message, location)))
        {
            var content = ConversationText.Sanitize(message.Content);
            if (content.Length > remaining) content = content[..remaining];
            if (content.Length == 0) continue;
            selected.Add(new(message.IsSelf ? "assistant" : "user", content));
            remaining -= content.Length;
            if (remaining == 0) break;
        }
        selected.Reverse();
        return selected.AsReadOnly();
    }

    bool Eligible(HistoryMessage message, ConversationLocation location) =>
        message.ChannelId == location.ChannelId && message.Id < location.MessageId &&
        message.CreatedAt < location.CreatedAt && message.CreatedAt >= time.GetUtcNow().AddMinutes(-15) &&
        !message.IsSystem && (!message.IsBot || message.IsSelf);
}
