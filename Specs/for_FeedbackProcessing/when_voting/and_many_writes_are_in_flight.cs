// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_many_writes_are_in_flight : given.a_feedback_processor
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _writes;
    int _peak;
    int _active;

    void Establish()
    {
        Create();
        Log.RecordFeedback(Arg.Any<long>(), Arg.Any<FeedbackVerdict>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _writes++;
            _active++;
            _peak = Math.Max(_peak, _active);
            await _release.Task.WaitAsync(call.Arg<CancellationToken>());
            _active--;
        });
    }

    async Task Because()
    {
        var first = Run(1, 1);
        var second = Run(2, 2);
        for (ulong user = 3; user <= 50; user++) await Run(user, (long)user);
        _release.SetResult();
        await Task.WhenAll(first, second);
    }

    [Fact] void should_bound_active_writes() => _peak.ShouldEqual(2);
    [Fact] void should_not_queue_any_more_writes() => _writes.ShouldEqual(2);
    [Fact] void should_not_acknowledge_rejected_clicks() => Acknowledgements.ShouldEqual(2);
    [Fact] void should_not_refuse_outside_the_global_budget() => Refusals.ShouldEqual(0);
}
