// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Cheap scope, payload, duplicate and rate checks shared by every Discord entry point.</summary>
/// <param name="options">The protection configuration.</param>
/// <param name="users">The existing per-user bucket.</param>
/// <param name="clock">The expiry clock.</param>
/// <param name="answering">The maintainer kill switch.</param>
public sealed class Admission(IOptions<PrompterOptions> options, RateLimiter users, TimeProvider clock, AnsweringSwitch answering)
{
    readonly Lock _lock = new();
    readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    readonly TransientLimits _windows = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _duplicates = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _replies = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _daily = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _cooldowns = new(options.Value.Protection.LocalCapacity);
    readonly EscalatingCooldowns _escalations = new(options.Value.Protection.LocalCapacity);

    /// <summary>Checks scope without reading history or fetching a forum starter.</summary>
    /// <param name="guild">The event guild, absent for DMs.</param>
    /// <param name="channel">The channel or parent forum.</param>
    /// <returns>Whether this is an enabled, approved source.</returns>
    public bool InScope(ulong? guild, ulong channel) =>
        options.Value.Protection.AnsweringEnabled && !answering.Paused && guild is { } id &&
        options.Value.Protection.AllowedGuilds.Contains(id) &&
        !options.Value.Protection.DeniedChannels.Contains(channel);

    /// <summary>Checks whether the user lists permit a user. Denial wins; an empty allow list permits everyone.</summary>
    /// <param name="user">The Discord user.</param>
    /// <returns>Whether the user may use the bot.</returns>
    public bool Permits(ulong user) =>
        !options.Value.Protection.DeniedUsers.Contains(user) &&
        (options.Value.Protection.AllowedUsers.Count == 0 || options.Value.Protection.AllowedUsers.Contains(user));

    /// <summary>Attempts cheap admission, retaining only keyed expiring hashes.</summary>
    /// <param name="actor">Transient Discord metadata.</param>
    /// <param name="question">The current question, never retained.</param>
    /// <returns>An admission decision.</returns>
    public AdmissionDecision TryEnter(DiscordActor actor, Question question)
    {
        if (!InScope(actor.GuildId, actor.ChannelId) || !Permits(actor.UserId))
        {
            ProviderMetrics.Event("admission", "denied");
            return new(false, false);
        }

        lock (_lock)
        {
            var now = clock.GetUtcNow();
            var userKey = Hash(actor.UserId.ToString(CultureInfo.InvariantCulture));
            var channelKey = Hash(actor.ChannelId.ToString(CultureInfo.InvariantCulture));
            var limits = options.Value.Protection;
            if (!actor.Maintainer && _escalations.Remaining(userKey.Value, now) is { } paused)
            {
                ProviderMetrics.Event("admission", "denied");
                return Deny(userKey, channelKey, now, AdmissionLimit.Escalated, paused);
            }

            if (!TryNormalize(question, out var normalized))
            {
                ProviderMetrics.Event("admission", "denied");
                return Deny(userKey, channelKey, now);
            }

            var duplicate = Hash($"{userKey.Value}:{channelKey.Value}:{normalized}");
            if (Limit(duplicate, userKey, channelKey, actor.Maintainer, now) is var (limit, wait))
            {
                ProviderMetrics.Event("admission", "denied");
                if (!actor.Maintainer && limit is AdmissionLimit.Duplicate or AdmissionLimit.Cooldown or AdmissionLimit.Daily or AdmissionLimit.Burst)
                {
                    _escalations.Strike(userKey.Value, now, limits);
                }

                return Deny(userKey, channelKey, now, limit, wait);
            }

            if (!actor.Maintainer)
            {
                _daily.Consume(userKey.Value, NextUtcMidnight(now) - now, now);
                _cooldowns.Consume(userKey.Value, TimeSpan.FromSeconds(limits.UserAdmissionCooldownSeconds), now);
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

    static DateTimeOffset NextUtcMidnight(DateTimeOffset now) => new(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);

    static (AdmissionLimit Limit, TimeSpan Wait) Waiting(AdmissionLimit limit, DateTimeOffset? expires, DateTimeOffset now) =>
        expires is { } at ? (limit, at - now) : (AdmissionLimit.Unknown, TimeSpan.Zero);

    /// <summary>Finds the first failing limit and its wait. The user bucket is checked last because it spends a token on success.</summary>
    /// <remarks>Maintainers skip the per-user limits but keep duplicate, global and channel checks.</remarks>
    (AdmissionLimit Limit, TimeSpan Wait)? Limit(AnonymousKey duplicate, AnonymousKey user, AnonymousKey channel, bool maintainer, DateTimeOffset now)
    {
        var limits = options.Value.Protection;
        if (!_duplicates.Available(duplicate.Value, 1, now))
        {
            return Waiting(AdmissionLimit.Duplicate, _duplicates.ExpiresAt(duplicate.Value, now), now);
        }

        if (!maintainer && !_daily.Available(user.Value, limits.UserAdmissionsPerDay, now))
        {
            return Waiting(AdmissionLimit.Daily, _daily.ExpiresAt(user.Value, now), now);
        }

        if (!maintainer && !_cooldowns.Available(user.Value, 1, now))
        {
            return Waiting(AdmissionLimit.Cooldown, _cooldowns.ExpiresAt(user.Value, now), now);
        }

        if (!_windows.Available("global", limits.GlobalPerHour, now))
        {
            return Waiting(AdmissionLimit.Busy, _windows.ExpiresAt("global", now), now);
        }

        if (!_windows.Available(channel.Value, limits.ChannelPerHour, now))
        {
            return Waiting(AdmissionLimit.Busy, _windows.ExpiresAt(channel.Value, now), now);
        }

        if (!maintainer && !users.TryConsume(user.Value, now))
        {
            var retry = users.RetryAfter(user.Value, now);
            return retry > TimeSpan.Zero ? (AdmissionLimit.Burst, retry) : (AdmissionLimit.Unknown, TimeSpan.Zero);
        }

        return null;
    }

    AdmissionDecision Deny(AnonymousKey user, AnonymousKey channel, DateTimeOffset now, AdmissionLimit limit = AdmissionLimit.Unknown, TimeSpan wait = default)
    {
        var limits = options.Value.Protection;
        var channelKey = $"channel:{channel.Value}";
        if (!_replies.Available(user.Value, 1, now) || !_replies.Available(channelKey, 1, now))
        {
            return new(false, false);
        }

        _replies.Consume(user.Value, TimeSpan.FromSeconds(limits.UserRefusalSeconds), now);
        _replies.Consume(channelKey, TimeSpan.FromSeconds(limits.ChannelRefusalSeconds), now);
        return new(false, true, RefusalReplies.For(limit, wait, limits.UserAdmissionsPerDay));
    }

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
