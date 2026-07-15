// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RateLimiter.when_consuming;

public class and_a_different_user_asks : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    RateLimiter _limiter = null!;
    bool _heavyUserDenied;
    bool _otherUserAllowed;

    void Establish()
    {
        _limiter = new RateLimiter(Options.Create(new PrompterOptions()));
        for (var i = 0; i < 5; i++)
        {
            _limiter.TryConsume("alice", _now);
        }
    }

    void Because()
    {
        _heavyUserDenied = _limiter.TryConsume("alice", _now);
        _otherUserAllowed = _limiter.TryConsume("bob", _now);
    }

    [Fact] void should_limit_the_user_who_exhausted_their_allowance() => _heavyUserDenied.ShouldBeFalse();
    [Fact] void should_track_each_user_independently() => _otherUserAllowed.ShouldBeTrue();
}
