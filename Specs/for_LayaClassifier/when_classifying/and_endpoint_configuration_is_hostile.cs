// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_endpoint_configuration_is_hostile : given.a_classifier
{
    readonly List<Classification> _results = [];

    async Task Because()
    {
        foreach (var address in new[]
        {
            "http://laya.internal/path", "http://laya.internal?debug=true", "http://laya.internal#route",
            "http://user@laya.internal", "ftp://laya.internal", "http://laya.internal.attacker.example",
            "http://169.254.169.254", "http://8.8.8.8", "https://public.example", "http://[2001:4860:4860::8888]"
        })
        {
            options.Endpoint = new(address);
            options.AllowedHost = options.Endpoint.DnsSafeHost;
            await Classify();
            _results.Add(result);
        }
        options.Endpoint = new("http://laya.internal");
        options.AllowedHost = "other.internal";
        await Classify();
        _results.Add(result);
    }

    [Fact] void should_reject_all_nonapproved_origins() => _results.TrueForAll(value => value.Fallback == ClassificationFallback.EndpointRejected).ShouldBeTrue();
    [Fact] void should_make_no_requests() => transport.Calls.ShouldEqual(0);
}
