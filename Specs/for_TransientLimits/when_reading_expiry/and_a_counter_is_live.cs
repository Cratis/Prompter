// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_TransientLimits.when_reading_expiry;

public class and_a_counter_is_live : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    TransientLimits _limits = null!;
    DateTimeOffset? _live;
    DateTimeOffset? _missing;
    DateTimeOffset? _expired;

    void Establish()
    {
        _limits = new(10);
        _limits.Consume("key", TimeSpan.FromSeconds(30), _now);
    }

    void Because()
    {
        _live = _limits.ExpiresAt("key", _now.AddSeconds(10));
        _missing = _limits.ExpiresAt("other", _now);
        _expired = _limits.ExpiresAt("key", _now.AddSeconds(30));
    }

    [Fact] void should_report_the_expiry() => _live.ShouldEqual(_now.AddSeconds(30));
    [Fact] void should_report_nothing_for_an_unknown_key() => _missing.ShouldBeNull();
    [Fact] void should_report_nothing_once_expired() => _expired.ShouldBeNull();
}
