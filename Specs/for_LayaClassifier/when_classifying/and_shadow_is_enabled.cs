// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_shadow_is_enabled : given.a_classifier
{
    async Task Because()
    {
        options.Mode = ClassificationMode.Shadow;
        options.ShadowPercentage = 10;
        for (var index = 0; index < 20; index++) await Classify();
    }

    [Fact] void should_sample_no_more_than_ten_percent() => transport.Calls.ShouldEqual(2);
    [Fact] void should_always_return_the_safe_baseline() => result.Action.ShouldEqual(ClassificationAction.Clarify);
    [Fact] void should_compare_with_metadata_only() => sink.Observations.Count.ShouldEqual(2);
}
