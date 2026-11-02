// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_acknowledgement_fails : given.a_feedback_processor
{
    Exception? _error;

    void Establish()
    {
        Options.Protection.FeedbackConcurrency = 1;
        Options.Protection.LocalCapacity = 1;
        Create();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => Processing.Run(1, FeedbackButton.CustomId(FeedbackVerdict.Up, 1), new(token => Task.FromCanceled(new CancellationToken(true)), Repeat, Refuse)));
        await Run(1, 2);
    }

    [Fact] void should_propagate_the_transport_fault() => _error.ShouldNotBeNull();
    [Fact] void should_release_all_work_and_verdict_capacity() => Acknowledgements.ShouldEqual(1);
    [Fact] async Task should_write_the_later_vote() => await Log.Received(1).RecordFeedback(2, FeedbackVerdict.Up, Arg.Any<CancellationToken>());
}
