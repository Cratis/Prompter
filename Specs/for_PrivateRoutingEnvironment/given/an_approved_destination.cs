// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_PrivateRoutingEnvironment.given;

public class an_approved_destination : Specification
{
    protected static readonly PrivateRoutingArgs Destination = new()
    {
        Endpoint = "http://classifier.routing-test.svc.cluster.local:80",
        AllowedHost = "classifier.routing-test.svc.cluster.local",
        Service = "classifier",
        Namespace = "routing-test",
        App = "classifier-pods",
        Environment = "test",
        BackendPort = 8080,
        Mode = "Active"
    };
}
