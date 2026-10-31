// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_endpoint_is_public : given.a_classifier
{
    Task Because()
    {
        options.Endpoint = new("https://8.8.8.8");
        options.AllowedHost = "8.8.8.8";
        return Classify();
    }

    [Fact] void should_not_make_a_request() => transport.Calls.ShouldEqual(0);
    [Fact] void should_return_safe_fallback() => result.Fallback.ShouldEqual(ClassificationFallback.EndpointRejected);
}
