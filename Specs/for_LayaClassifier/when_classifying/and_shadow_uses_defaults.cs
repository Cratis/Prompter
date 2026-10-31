// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_shadow_uses_defaults : given.a_classifier
{
    async Task Because()
    {
        options.Mode = ClassificationMode.Shadow;
        for (var index = 0; index < 100; index++) await Classify();
    }

    [Fact] void should_be_off_by_default() => transport.Calls.ShouldEqual(0);
    [Fact] void should_not_emit_unsampled_provider_metadata() => sink.Observations.Count.ShouldEqual(0);
}
