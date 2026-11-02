// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;
using NetCord;

namespace Cratis.Prompter.Discord;

/// <summary>Coordinates nonpaid actions independently of answer admission and spend.</summary>
/// <param name="admission">Guild/channel scope and suppressed refusal replies only.</param>
/// <param name="options">Independent action quotas and bounded local capacity.</param>
/// <param name="clock">The deadline and window clock.</param>
public sealed class NonpaidActions(Admission admission, IOptions<PrompterOptions> options, TimeProvider clock)
{
    readonly Lock _gate = new();
    readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    readonly BoundedWork _work = new(options.Value.Protection.ActionConcurrency, options.Value.Protection.ActionPerMinute, clock);
    readonly TransientLimits _users = new(options.Value.Protection.LocalCapacity);
    readonly TransientLimits _channels = new(options.Value.Protection.LocalCapacity);

    /// <summary>Reserves a bounded action before any acknowledgement, lookup or delivery.</summary>
    /// <param name="actor">The clicking user and location.</param>
    /// <param name="channel">Trusted channel metadata, including the parent of a thread.</param>
    /// <param name="callbacks">The transport callbacks, all covered by the same reservation.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The bounded interaction lifecycle, or immediate completion on silent rejection.</returns>
    public async Task Run(
        DiscordActor actor,
        TextChannel? channel,
        NonpaidCallbacks callbacks,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested || !Trusted(actor, channel)) return;
        using var turn = _work.TryEnter();
        if (turn is null) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            if (!Spend(actor))
            {
                if (admission.MayReply(actor)) await callbacks.Refuse(options.Value.Discord.RateLimitedReply, deadline.Token);
                return;
            }
            await callbacks.Defer(deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            var reply = await callbacks.Work(deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            await callbacks.Followup(reply, deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            // The reservation covers acknowledgement, downstream work and follow-up together.
        }
    }

    bool Spend(DiscordActor actor)
    {
        var limits = options.Value.Protection;
        var user = Hash($"user:{actor.UserId}");
        var channel = Hash($"channel:{actor.ChannelId}");
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            if (!Available(user, channel, now)) return false;
            _users.Consume(user, TimeSpan.FromSeconds(limits.ActionWindowSeconds), now);
            _channels.Consume(channel, TimeSpan.FromMinutes(1), now);
            return true;
        }
    }

    bool Available(string user, string channel, DateTimeOffset now) =>
        _users.Available(user, options.Value.Protection.ActionClicksPerUser, now) &&
        _channels.Available(channel, options.Value.Protection.ActionChannelPerMinute, now);

    bool Trusted(DiscordActor actor, TextChannel? channel) => channel is not null && channel.Id == actor.ChannelId && actor.UserId != 0 && ChannelAdmission.InScope(admission, actor, channel);

    string Hash(string value) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));
}
