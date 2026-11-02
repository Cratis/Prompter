// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_EscalatingCooldowns.when_striking;

public class and_refusals_repeat : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    readonly ProtectionOptions _options = new() { RefusalStrikes = 3, RefusalStrikeWindowSeconds = 60, EscalatedCooldownSeconds = 100, MaxEscalatedCooldownSeconds = 300 };
    EscalatingCooldowns _cooldowns = null!;
    bool _belowThreshold;
    bool _started;
    TimeSpan? _first;
    TimeSpan? _afterExpiry;
    TimeSpan? _second;
    TimeSpan? _third;
    bool _strikeDuringBlock;
    TimeSpan? _blockAfterIgnoredStrike;

    void Establish() => _cooldowns = new(10);

    void Because()
    {
        _belowThreshold = _cooldowns.Strike("user", _now, _options) || _cooldowns.Strike("user", _now, _options);
        _started = _cooldowns.Strike("user", _now, _options);
        _first = _cooldowns.Remaining("user", _now);
        _strikeDuringBlock = _cooldowns.Strike("user", _now.AddSeconds(50), _options);
        _blockAfterIgnoredStrike = _cooldowns.Remaining("user", _now.AddSeconds(50));
        _afterExpiry = _cooldowns.Remaining("user", _now.AddSeconds(100));
        var later = _now.AddSeconds(101);
        Repeat(later);
        _second = _cooldowns.Remaining("user", later);
        later = later.AddSeconds(201);
        Repeat(later);
        _third = _cooldowns.Remaining("user", later);
    }

    void Repeat(DateTimeOffset at)
    {
        for (var i = 0; i < 3; i++)
        {
            _cooldowns.Strike("user", at, _options);
        }
    }

    [Fact] void should_not_pause_below_the_threshold() => _belowThreshold.ShouldBeFalse();
    [Fact] void should_start_a_pause_at_the_threshold() => _started.ShouldBeTrue();
    [Fact] void should_use_the_first_cooldown() => _first.ShouldEqual(TimeSpan.FromSeconds(100));
    [Fact] void should_not_count_a_refusal_during_a_pause() => _strikeDuringBlock.ShouldBeFalse();
    [Fact] void should_not_extend_a_pause() => _blockAfterIgnoredStrike.ShouldEqual(TimeSpan.FromSeconds(50));
    [Fact] void should_end_the_pause() => _afterExpiry.ShouldBeNull();
    [Fact] void should_double_the_next_pause() => _second.ShouldEqual(TimeSpan.FromSeconds(200));
    [Fact] void should_cap_the_pause() => _third.ShouldEqual(TimeSpan.FromSeconds(300));
}
