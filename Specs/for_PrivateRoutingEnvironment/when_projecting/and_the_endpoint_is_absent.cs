// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_PrivateRoutingEnvironment.when_projecting;

public class and_the_endpoint_is_absent : given.an_approved_destination
{
    [Fact]
    void should_leave_remote_routing_and_egress_off()
    {
        foreach (var disabled in new[] { new PrivateRoutingArgs(), Destination with { Endpoint = string.Empty } })
        {
            var values = PrivateRoutingEnvironment.Values(disabled);
            values["Cratis__Prompter__Routing__Endpoint"].ShouldEqual(string.Empty);
            values["Cratis__Prompter__Routing__AllowedHost"].ShouldEqual(string.Empty);
            values["Cratis__Prompter__Routing__ShadowPercentage"].ShouldEqual("0");
            PrivateRoutingEgress.Create(disabled, "test").ShouldBeNull();
        }
    }
}
