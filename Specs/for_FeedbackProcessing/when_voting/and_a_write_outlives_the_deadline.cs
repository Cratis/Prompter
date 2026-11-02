// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_a_write_outlives_the_deadline : given.a_feedback_processor
{
    CancellationToken _received;
    int _writes;

    void Establish()
    {
        Options.Protection.FeedbackConcurrency = 1;
        Create();
        Log.RecordFeedback(Arg.Any<long>(), Arg.Any<FeedbackVerdict>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _writes++;
            if (_writes == 1)
            {
                _received = call.Arg<CancellationToken>();
                await new TaskCompletionSource().Task.WaitAsync(_received);
            }
        });
    }

    async Task Because()
    {
        var pending = Run(1, 1);
        Clock.Advance(TimeSpan.FromSeconds(2));
        await pending;
        await Run(1, 1);
    }

    [Fact] void should_pass_a_cancellable_write_token() => _received.CanBeCanceled.ShouldBeTrue();
    [Fact] void should_cancel_the_write() => _received.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_release_capacity_and_not_cache_the_failed_verdict() => _writes.ShouldEqual(2);
    [Fact] void should_acknowledge_the_retry() => Acknowledgements.ShouldEqual(2);
    [Fact] void should_not_treat_the_retry_as_a_repeat() => Repeats.ShouldEqual(0);
}
