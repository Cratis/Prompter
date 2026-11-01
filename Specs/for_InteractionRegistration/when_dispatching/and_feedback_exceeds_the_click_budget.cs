// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_feedback_exceeds_the_click_budget : given.a_button_dispatcher
{
    async Task Because()
    {
        Options.Protection.FeedbackClicksPerUser = 1;
        Options.Discord.RateLimitedReply = "Slow down please.";
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));
    }

    [Fact] void should_not_record_the_excess_click() => Interactions.DidNotReceive().RecordFeedback(42, FeedbackVerdict.Down, Arg.Any<CancellationToken>());
    [Fact] void should_refuse_once() => Payloads.Count(payload => payload.Contains(Options.Discord.RateLimitedReply, StringComparison.Ordinal)).ShouldEqual(1);
}
