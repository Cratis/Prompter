// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_storage_completes_after_cancellation : given.a_feedback_processor
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _writes;

    void Establish()
    {
        Create();
        Log.RecordFeedback(Arg.Any<long>(), Arg.Any<FeedbackVerdict>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _writes++;
            if (_writes == 1) await _release.Task;
        });
    }

    async Task Because()
    {
        var first = Run(1, 1);
        Clock.Advance(TimeSpan.FromSeconds(2));
        _release.SetResult();
        await first;
        await Run(1, 1);
    }

    [Fact] void should_not_cache_a_write_that_returned_after_cancellation() => _writes.ShouldEqual(2);
    [Fact] void should_release_the_turn_for_a_later_attempt() => Acknowledgements.ShouldEqual(2);
    [Fact] void should_not_treat_the_later_attempt_as_a_repeat() => Repeats.ShouldEqual(0);
}
