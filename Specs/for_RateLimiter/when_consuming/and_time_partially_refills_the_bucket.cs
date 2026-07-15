// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RateLimiter.when_consuming;

public class and_time_partially_refills_the_bucket : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    RateLimiter _limiter = null!;
    bool _firstAfterPartialRefill;
    bool _secondAfterPartialRefill;

    void Establish()
    {
        _limiter = new RateLimiter(Options.Create(new PrompterOptions()));
        for (var i = 0; i < 5; i++)
        {
            _limiter.TryConsume("user", _now);
        }
    }

    void Because()
    {
        // Default 5 questions / 10 minutes refills one slot every two minutes; three minutes yields ~1.5 slots.
        var later = _now.AddMinutes(3);
        _firstAfterPartialRefill = _limiter.TryConsume("user", later);
        _secondAfterPartialRefill = _limiter.TryConsume("user", later);
    }

    [Fact] void should_allow_the_single_refilled_slot() => _firstAfterPartialRefill.ShouldBeTrue();
    [Fact] void should_deny_once_that_slot_is_spent() => _secondAfterPartialRefill.ShouldBeFalse();
}
