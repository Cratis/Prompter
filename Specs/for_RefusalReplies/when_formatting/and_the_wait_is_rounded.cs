// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_RefusalReplies.when_formatting;

public class and_the_wait_is_rounded : Specification
{
    string? _seconds;
    string? _singleSecond;
    string? _minutes;
    string? _hours;

    void Because()
    {
        _seconds = RefusalReplies.For(AdmissionLimit.Cooldown, TimeSpan.FromSeconds(11.2), 30);
        _singleSecond = RefusalReplies.For(AdmissionLimit.Cooldown, TimeSpan.FromMilliseconds(200), 30);
        _minutes = RefusalReplies.For(AdmissionLimit.Burst, TimeSpan.FromSeconds(121), 30);
        _hours = RefusalReplies.For(AdmissionLimit.Escalated, TimeSpan.FromHours(3.2), 30);
    }

    [Fact] void should_round_seconds_up() => _seconds.ShouldContain("about 12 seconds");
    [Fact] void should_never_say_zero() => _singleSecond.ShouldContain("about 1 second.");
    [Fact] void should_round_minutes_up() => _minutes.ShouldContain("about 3 minutes");
    [Fact] void should_round_hours_up() => _hours.ShouldContain("about 4 hours");
}
