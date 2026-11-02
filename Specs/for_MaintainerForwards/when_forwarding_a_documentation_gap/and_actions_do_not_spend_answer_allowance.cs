// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_actions_do_not_spend_answer_allowance : given.a_forward
{
    bool _hostileMayAnswer;
    bool _successfulMayAnswer;
    bool _actionDenialStillAllowsAnswer;
    string _hostileReply = string.Empty;
    string _successfulReply = string.Empty;
    int _actionDeniedReads;

    async Task Because()
    {
        Options.Discord.RateLimit.MaxQuestions = 1;
        Options.Protection.GlobalPerHour = 1;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        var clock = new ControlledTime(Clock.GetUtcNow());
        var admission = new Admission(config, new RateLimiter(config), clock, Fakes.Switch.Running());
        var actions = new NonpaidActions(admission, config, clock);
        using var rest = Discord.Client();
        var forwards = new MaintainerForwards(rest, Channel, admission, config, clock, NullLogger<MaintainerForwards>.Instance);
        async Task<string> RunAction(ForwardClick click)
        {
            var result = string.Empty;
            Task Capture(string text, CancellationToken token)
            {
                result = text;
                return Task.CompletedTask;
            }
            await actions.Run(click.Actor, click.Channel, new(_ => Task.CompletedTask, token => forwards.Forward(click, token), Capture, Capture), TestContext.Current.CancellationToken);
            return result;
        }
        var hostile = Documented with { Actor = Documented.Actor with { UserId = 4 } };
        _hostileReply = await RunAction(hostile);
        _hostileMayAnswer = admission.TryEnter(hostile.Actor, new("ordinary question")).Allowed;
        _successfulReply = await RunAction(Documented);
        clock.Advance(TimeSpan.FromHours(1));
        _successfulMayAnswer = admission.TryEnter(Documented.Actor, new("another question")).Allowed;
        Options.Protection.ActionClicksPerUser = 0;
        var reads = Discord.Reads;
        await RunAction(Documented with { Actor = Documented.Actor with { UserId = 5 } });
        _actionDeniedReads = Discord.Reads - reads;
        clock.Advance(TimeSpan.FromHours(1));
        _actionDenialStillAllowsAnswer = admission.TryEnter(new(1, 2, 5), new("separate question")).Allowed;
    }

    [Fact] void should_reject_the_other_user_without_delivery() => _hostileReply.ShouldEqual(MaintainerForwards.OnlyAskerReply);
    [Fact] void should_leave_the_other_users_answer_allowance_untouched() => _hostileMayAnswer.ShouldBeTrue();
    [Fact] void should_forward_even_after_answer_global_capacity_is_exhausted() => _successfulReply.ShouldEqual(MaintainerForwards.ForwardedReply);
    [Fact] void should_not_charge_the_successful_askers_answer_allowance() => _successfulMayAnswer.ShouldBeTrue();
    [Fact] void should_not_fetch_a_question_after_action_quota_exhaustion() => _actionDeniedReads.ShouldEqual(0);
    [Fact] void should_allow_answers_after_action_quota_exhaustion() => _actionDenialStillAllowsAnswer.ShouldBeTrue();
    [Fact] void should_post_only_the_askers_consented_action() => Channel.Received(1).Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
