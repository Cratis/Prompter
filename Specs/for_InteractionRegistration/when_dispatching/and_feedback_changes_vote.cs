// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_feedback_changes_vote : given.a_button_dispatcher
{
    async Task Because()
    {
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));
    }

    [Fact] void should_record_the_first_verdict() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Up, Arg.Any<CancellationToken>());
    [Fact] void should_record_the_changed_verdict() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Down, Arg.Any<CancellationToken>());
}
