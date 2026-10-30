// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_response_is_invalid : given.a_classifier
{
    Task Because()
    {
        transport.Change = _ => "{\"results\":[]}";
        return Classify();
    }

    [Fact] void should_use_the_deterministic_product() => result.Product.Value.ShouldEqual("arc");
    [Fact] void should_never_offer_filing() => result.Action.ShouldEqual(ClassificationAction.Clarify);
    [Fact] void should_mark_invalid_response() => result.Fallback.ShouldEqual(ClassificationFallback.InvalidResponse);
}
