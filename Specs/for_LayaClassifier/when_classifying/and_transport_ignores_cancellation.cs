// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_transport_ignores_cancellation : given.a_classifier
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Classification _second = null!;

    async Task Because()
    {
        transport.Delay = _ => _release.Task;
        await Classify();
        _second = await classifier.Classify("Arc", []);
        _release.SetResult();
    }

    [Fact] void should_return_after_the_deadline() => result.Fallback.ShouldEqual(ClassificationFallback.Timeout);
    [Fact] void should_not_accumulate_abandoned_operations() => transport.Calls.ShouldEqual(1);
    [Fact] void should_fall_back_while_transport_is_still_owned() => _second.Fallback.ShouldEqual(ClassificationFallback.Busy);
}
