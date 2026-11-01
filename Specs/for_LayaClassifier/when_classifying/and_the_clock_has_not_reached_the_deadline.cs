// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_clock_has_not_reached_the_deadline : given.a_classifier
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedBeforeDeadline;

    async Task Because()
    {
        transport.Delay = _ => _release.Task;
        var pending = classifier.Classify("Arc is broken", []);
        clock.Advance(TimeSpan.FromMilliseconds(999));
        _completedBeforeDeadline = pending.IsCompleted;
        clock.Advance(TimeSpan.FromMilliseconds(1));
        result = await pending;
        _release.SetResult();
    }

    [Fact] void should_still_wait_for_laya() => _completedBeforeDeadline.ShouldBeFalse();
    [Fact] void should_time_out_at_one_second() => result.Fallback.ShouldEqual(ClassificationFallback.Timeout);
}
