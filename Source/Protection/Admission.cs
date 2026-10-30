// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Cheap scope, payload, duplicate and rate checks shared by every Discord entry point.</summary>
/// <param name="options">The protection configuration.</param>
/// <param name="users">The existing per-user bucket.</param>
/// <param name="clock">The expiry clock.</param>
public sealed class Admission(IOptions<PrompterOptions> options, RateLimiter users, TimeProvider clock)
{
    readonly Lock _lock = new();
    readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    readonly TransientLimits _windows = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _duplicates = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _replies = new(options.Value.Protection.LocalCapacity);

    /// <summary>Checks scope without reading history or fetching a forum starter.</summary>
    /// <param name="guild">The event guild, absent for DMs.</param>
    /// <param name="channel">The channel or parent forum.</param>
    /// <returns>Whether this is an enabled, approved source.</returns>
    public bool InScope(ulong? guild, ulong channel) =>
        options.Value.Protection.AnsweringEnabled && guild is { } id &&
        options.Value.Protection.AllowedGuilds.Contains(id) &&
        !options.Value.Protection.DeniedChannels.Contains(channel);

    /// <summary>Attempts cheap admission, retaining only keyed expiring hashes.</summary>
    /// <param name="actor">Transient Discord metadata.</param>
    /// <param name="question">The current question, never retained.</param>
    /// <returns>An admission decision.</returns>
    public AdmissionDecision TryEnter(DiscordActor actor, Question question)
    {
        if (!InScope(actor.GuildId, actor.ChannelId))
        {
            return new(false, false);
        }

        lock (_lock)
        {
            var now = clock.GetUtcNow();
            var userKey = Hash(actor.UserId.ToString(CultureInfo.InvariantCulture));
            var channelKey = Hash(actor.ChannelId.ToString(CultureInfo.InvariantCulture));
            var limits = options.Value.Protection;
            if (!TryNormalize(question, out var normalized))
            {
                return Deny(userKey, channelKey, now);
            }

            var duplicate = Hash($"{userKey.Value}:{channelKey.Value}:{normalized}");
            if (!_duplicates.Available(duplicate.Value, 1, now) || !UnderRateLimits(userKey, channelKey, now))
            {
                return Deny(userKey, channelKey, now);
            }

            _duplicates.Consume(duplicate.Value, TimeSpan.FromSeconds(limits.DuplicateSeconds), now);
            _windows.Consume("global", TimeSpan.FromHours(1), now);
            _windows.Consume(channelKey.Value, TimeSpan.FromHours(1), now);
            return new(true, false);
        }
    }

    /// <summary>Throttles replies for a later concurrency or budget rejection.</summary>
    /// <param name="actor">Transient Discord metadata.</param>
    /// <returns>Whether a refusal may be sent.</returns>
    public bool MayReply(DiscordActor actor)
    {
        lock (_lock)
        {
            return Deny(Hash(actor.UserId.ToString(CultureInfo.InvariantCulture)), Hash(actor.ChannelId.ToString(CultureInfo.InvariantCulture)), clock.GetUtcNow()).Reply;
        }
    }

    AdmissionDecision Deny(AnonymousKey user, AnonymousKey channel, DateTimeOffset now)
    {
        var limits = options.Value.Protection;
        var channelKey = $"channel:{channel.Value}";
        if (!_replies.Available(user.Value, 1, now) || !_replies.Available(channelKey, 1, now))
        {
            return new(false, false);
        }

        _replies.Consume(user.Value, TimeSpan.FromSeconds(limits.UserRefusalSeconds), now);
        _replies.Consume(channelKey, TimeSpan.FromSeconds(limits.ChannelRefusalSeconds), now);
        return new(false, true);
    }

    bool UnderRateLimits(AnonymousKey user, AnonymousKey channel, DateTimeOffset now) =>
        _windows.Available("global", options.Value.Protection.GlobalPerHour, now) &&
        _windows.Available(channel.Value, options.Value.Protection.ChannelPerHour, now) &&
        users.TryConsume(user.Value, now);

    bool TryNormalize(Question question, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(question.Value) || question.Value.Length > options.Value.Protection.MaxQuestionCharacters)
        {
            return false;
        }

        try
        {
            normalized = string.Join(' ', question.Value.Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    AnonymousKey Hash(string value) => new(Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value))));

    sealed record AnonymousKey(string Value);
}
