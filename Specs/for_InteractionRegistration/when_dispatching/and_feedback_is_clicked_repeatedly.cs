// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_feedback_is_clicked_repeatedly : given.a_button_dispatcher
{
    async Task Because()
    {
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
    }

    [Fact] void should_record_it_once() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Up, Arg.Any<CancellationToken>());
    [Fact] void should_say_thanks_once() => Payloads.Count(payload => payload.Contains(Feedback.Acknowledgement, StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_acknowledge_the_repeat_silently() => Payloads.Last().Replace(" ", string.Empty).ShouldContain("\"type\":6");
}
