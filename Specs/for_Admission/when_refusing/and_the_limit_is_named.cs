// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_refusing;

public class and_the_limit_is_named : given.an_admission
{
    AdmissionDecision _cooldown = null!;
    AdmissionDecision _throttled = null!;
    AdmissionDecision _duplicate = null!;
    AdmissionDecision _burst = null!;
    AdmissionDecision _daily = null!;
    AdmissionDecision _busy = null!;

    void Establish()
    {
        Options.Protection.UserAdmissionsPerDay = 100;
        Options.Protection.UserAdmissionCooldownSeconds = 15;
        Options.Protection.ChannelPerHour = 100;
        Options.Protection.DuplicateSeconds = 120;
        Options.Protection.UserRefusalSeconds = 1;
        Options.Protection.ChannelRefusalSeconds = 1;
        Options.Discord.RateLimit.MaxQuestions = 2;
    }

    void Because()
    {
        Guard.TryEnter(new(123, 1, 1), "first");
        _cooldown = Guard.TryEnter(new(123, 1, 1), "second");
        _throttled = Guard.TryEnter(new(123, 1, 1), "second");

        Guard.TryEnter(new(123, 2, 2), "same");
        Clock.Advance(TimeSpan.FromSeconds(20));
        _duplicate = Guard.TryEnter(new(123, 2, 2), "same");

        Guard.TryEnter(new(123, 3, 3), "a");
        Clock.Advance(TimeSpan.FromSeconds(16));
        Guard.TryEnter(new(123, 3, 3), "b");
        Clock.Advance(TimeSpan.FromSeconds(16));
        _burst = Guard.TryEnter(new(123, 3, 3), "c");

        Options.Protection.UserAdmissionsPerDay = 1;
        Guard.TryEnter(new(123, 4, 4), "x");
        Clock.Advance(TimeSpan.FromSeconds(16));
        _daily = Guard.TryEnter(new(123, 4, 4), "y");

        Options.Protection.ChannelPerHour = 1;
        Guard.TryEnter(new(123, 5, 5), "p");
        _busy = Guard.TryEnter(new(123, 5, 6), "q");
    }

    [Fact] void should_name_the_cooldown_and_the_wait() => _cooldown.Text.ShouldContain("about 15 seconds");
    [Fact] void should_still_throttle_the_second_reply() => _throttled.Reply.ShouldBeFalse();
    [Fact] void should_name_a_duplicate() => _duplicate.Text.ShouldContain("same question");
    [Fact] void should_name_the_burst_limit() => _burst.Text.ShouldContain("a lot of questions in a short time");
    [Fact] void should_name_the_daily_allowance_and_its_reset() => _daily.Text.ShouldContain("00:00 UTC");
    [Fact] void should_name_a_busy_channel() => _busy.Text.ShouldContain("answering a lot of questions");
}
