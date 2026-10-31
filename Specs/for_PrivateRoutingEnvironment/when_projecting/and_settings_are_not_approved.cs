// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_PrivateRoutingEnvironment.when_projecting;

public class and_settings_are_not_approved : given.an_approved_destination
{
    [Fact]
    void should_refuse_partial_hostile_or_implicitly_active_configuration_without_a_grant()
    {
        foreach (var invalid in new[]
        {
            new PrivateRoutingArgs { Endpoint = Destination.Endpoint },
            Destination with { Namespace = "other" },
            Destination with { Service = "other" },
            Destination with { App = string.Empty },
            Destination with { Environment = string.Empty },
            Destination with { BackendPort = 0 },
            Destination with { BackendPort = 65536 },
            Destination with { AllowedHost = "other.routing-test.svc.cluster.local" },
            Destination with { Endpoint = "http://public.example:80" },
            Destination with { Endpoint = "http://127.0.0.1:80" },
            Destination with { Endpoint = "http://169.254.169.254:80" },
            Destination with { Endpoint = Destination.Endpoint + "/v1/decisions" },
            Destination with { Endpoint = Destination.Endpoint + "?key=example" },
            Destination with { Endpoint = Destination.Endpoint + "#fragment" },
            Destination with { Endpoint = "http://user@" + Destination.AllowedHost + ":80" },
            Destination with { Endpoint = "http://" + Destination.AllowedHost + ".example:80" },
            Destination with { Mode = string.Empty },
            Destination with { Mode = "2" },
            Destination with { Mode = "Unknown" },
            Destination with { MinimumConfidence = double.NaN },
            Destination with { MinimumConfidence = double.PositiveInfinity },
            Destination with { MinimumConfidence = -0.1 },
            Destination with { MinimumMargin = double.NegativeInfinity },
            Destination with { MinimumMargin = 1.1 }
        })
        {
            invalid.IsValid.ShouldBeFalse();
            PrivateRoutingEgress.Create(invalid, "test").ShouldBeNull();
            Catch.Exception(() => PrivateRoutingEnvironment.Values(invalid)).ShouldBeOfExactType<InvalidPrivateRouting>();
        }
    }
}
