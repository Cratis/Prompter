// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RateLimiter.when_consuming;

public class and_the_window_has_passed : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    RateLimiter _limiter = null!;
    int _exhausted;
    bool _deniedWithinWindow;
    bool _allowedAfterWindow;

    void Establish() => _limiter = new RateLimiter(Options.Create(new PrompterOptions()));

    void Because()
    {
        _exhausted = Enumerable.Range(0, 5).Count(_ => _limiter.TryConsume("user", _now));
        _deniedWithinWindow = _limiter.TryConsume("user", _now);
        _allowedAfterWindow = _limiter.TryConsume("user", _now.AddMinutes(10));
    }

    [Fact] void should_have_spent_the_whole_allowance() => _exhausted.ShouldEqual(5);
    [Fact] void should_deny_before_the_window_elapses() => _deniedWithinWindow.ShouldBeFalse();
    [Fact] void should_allow_once_the_window_refills() => _allowedAfterWindow.ShouldBeTrue();
}
