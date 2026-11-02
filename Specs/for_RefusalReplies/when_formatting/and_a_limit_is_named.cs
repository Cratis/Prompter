// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_RefusalReplies.when_formatting;

public class and_a_limit_is_named : Specification
{
    string? _daily;
    string? _duplicate;
    string? _escalated;
    string? _busy;
    string? _unknown;

    void Because()
    {
        _daily = RefusalReplies.For(AdmissionLimit.Daily, TimeSpan.FromHours(4), 30);
        _duplicate = RefusalReplies.For(AdmissionLimit.Duplicate, TimeSpan.FromSeconds(90), 30);
        _escalated = RefusalReplies.For(AdmissionLimit.Escalated, TimeSpan.FromMinutes(20), 30);
        _busy = RefusalReplies.For(AdmissionLimit.Busy, TimeSpan.FromMinutes(10), 30);
        _unknown = RefusalReplies.For(AdmissionLimit.Unknown, TimeSpan.Zero, 30);
    }

    [Fact] void should_name_the_daily_allowance_and_reset() => _daily.ShouldEqual("You've used today's 30 questions. Your allowance resets at 00:00 UTC (in about 4 hours).");
    [Fact] void should_name_a_duplicate() => _duplicate.ShouldEqual("I just got that same question from you — ask again in about 90 seconds if you still need it.");
    [Fact] void should_name_an_escalated_pause() => _escalated.ShouldEqual("You've hit the limits several times, so I'm pausing your questions for about 20 minutes.");
    [Fact] void should_name_a_busy_channel() => _busy.ShouldEqual("I'm answering a lot of questions here right now — try again in about 10 minutes.");
    [Fact] void should_fall_back_to_the_generic_reply_when_unknown() => _unknown.ShouldBeNull();
}
