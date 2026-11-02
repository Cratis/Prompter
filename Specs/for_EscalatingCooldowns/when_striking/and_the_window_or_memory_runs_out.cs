// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_EscalatingCooldowns.when_striking;

public class and_the_window_or_memory_runs_out : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    readonly ProtectionOptions _options = new() { RefusalStrikes = 2, RefusalStrikeWindowSeconds = 60, EscalatedCooldownSeconds = 100, MaxEscalatedCooldownSeconds = 1000 };
    EscalatingCooldowns _cooldowns = null!;
    bool _afterWindow;
    bool _full;
    TimeSpan? _fullRemaining;
    TimeSpan? _afterForgotten;
    bool _disabled;

    void Establish() => _cooldowns = new(1);

    void Because()
    {
        _cooldowns.Strike("user", _now, _options);
        _afterWindow = _cooldowns.Strike("user", _now.AddSeconds(61), _options);
        _full = _cooldowns.Strike("other", _now.AddSeconds(61), _options) || _cooldowns.Strike("other", _now.AddSeconds(61), _options);
        _fullRemaining = _cooldowns.Remaining("other", _now.AddSeconds(61));
        _cooldowns.Strike("user", _now.AddSeconds(62), _options);
        var forgotten = _now.AddSeconds(62 + 100 + (24 * 3600) + 1);
        _afterForgotten = _cooldowns.Remaining("user", forgotten);
        _disabled = _cooldowns.Strike("user", forgotten, new() { RefusalStrikes = 0 });
    }

    [Fact] void should_forget_strikes_outside_the_window() => _afterWindow.ShouldBeFalse();
    [Fact] void should_not_track_a_new_requester_when_full() => _full.ShouldBeFalse();
    [Fact] void should_not_pause_an_untracked_requester() => _fullRemaining.ShouldBeNull();
    [Fact] void should_forget_the_level_after_a_day() => _afterForgotten.ShouldBeNull();
    [Fact] void should_not_strike_when_disabled() => _disabled.ShouldBeFalse();
}
