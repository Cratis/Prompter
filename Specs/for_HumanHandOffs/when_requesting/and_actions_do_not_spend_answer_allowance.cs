// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_actions_do_not_spend_answer_allowance : given.a_hand_off
{
    string _hostile = string.Empty;
    string _successful = string.Empty;
    bool _hostileMayAnswer;
    bool _successfulMayAnswer;
    bool _deniedMayAnswer;
    int _hostilePings;
    int _deniedReads;

    void Establish()
    {
        Options.Discord.MaintainerRoleId = 6;
        Options.Discord.RateLimit.MaxQuestions = 1;
        Options.Protection.GlobalPerHour = 1;
        Options.Protection.UserAdmissionsPerDay = 1;
    }

    async Task Because()
    {
        var hostile = Click with { Actor = Click.Actor with { UserId = 7 } };
        _hostile = await Run(hostile);
        _hostilePings = Discord.Writes;
        _hostileMayAnswer = Admission.TryEnter(hostile.Actor, "ordinary question").Allowed;
        _successful = await Run(Click);
        Clock.Advance(TimeSpan.FromHours(1));
        _successfulMayAnswer = Admission.TryEnter(Click.Actor, "another question").Allowed;
        Options.Protection.ActionClicksPerUser = 0;
        var reads = Discord.Reads;
        await Run(Click with { Actor = Click.Actor with { UserId = 8 } });
        _deniedReads = Discord.Reads - reads;
        Clock.Advance(TimeSpan.FromHours(1));
        _deniedMayAnswer = Admission.TryEnter(Click.Actor with { UserId = 8 }, "separate question").Allowed;
    }

    [Fact] void should_reject_another_users_click() => _hostile.ShouldEqual(HumanHandOffs.OnlyAskerReply);
    [Fact] void should_never_ping_for_another_users_click() => _hostilePings.ShouldEqual(0);
    [Fact] void should_leave_the_other_users_answer_allowance_untouched() => _hostileMayAnswer.ShouldBeTrue();
    [Fact] void should_allow_a_legitimate_hand_off_after_global_answer_exhaustion() => _successful.ShouldEqual(HumanHandOffs.RequestedReply);
    [Fact] void should_leave_the_askers_answer_allowance_untouched() => _successfulMayAnswer.ShouldBeTrue();
    [Fact] void should_fetch_no_message_after_action_denial() => _deniedReads.ShouldEqual(0);
    [Fact] void should_allow_an_answer_after_action_denial() => _deniedMayAnswer.ShouldBeTrue();
    [Fact] void should_ping_only_for_the_askers_consented_click() => Discord.Writes.ShouldEqual(1);
}
