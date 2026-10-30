// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_no_endpoint_is_configured : given.a_classifier
{
    Task Because()
    {
        options.Endpoint = null;
        return Classify("the architecture of Arc");
    }

    [Fact] void should_not_probe_any_service() => transport.Calls.ShouldEqual(0);
    [Fact] void should_match_only_a_whole_product_name() => result.Product.Value.ShouldEqual("arc");
    [Fact] void should_remain_a_question() => result.Intent.ShouldEqual(ClassificationIntent.Question);
    [Fact] void should_never_offer_filing() => result.Action.ShouldEqual(ClassificationAction.Clarify);
}
