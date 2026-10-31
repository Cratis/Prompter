// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_service_redirects : given.a_classifier
{
    Task Because()
    {
        transport.Status = HttpStatusCode.Redirect;
        return Classify();
    }

    [Fact] void should_not_follow_or_retry_the_response() => transport.Calls.ShouldEqual(1);
    [Fact] void should_use_safe_fallback() => result.Fallback.ShouldEqual(ClassificationFallback.Unavailable);
}
