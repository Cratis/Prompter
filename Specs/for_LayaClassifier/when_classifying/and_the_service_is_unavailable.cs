// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_service_is_unavailable : given.a_classifier
{
    Task Because()
    {
        transport.Status = HttpStatusCode.ServiceUnavailable;
        return Classify("Arc and Chronicle");
    }

    [Fact] void should_not_retry() => transport.Calls.ShouldEqual(1);
    [Fact] void should_have_no_paid_fallback() => result.Source.ShouldEqual(ClassificationSource.Deterministic);
    [Fact] void should_report_only_a_bounded_reason() => result.Fallback.ShouldEqual(ClassificationFallback.Unavailable);
    [Fact] void should_keep_ambiguous_product_unknown() => result.Product.ShouldEqual(RoutingProduct.Unknown);
}
