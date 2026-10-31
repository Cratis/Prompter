// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_PrivateMetricsEnvironment.given;

public class an_approved_collector : Specification
{
    protected static readonly PrivateMetricsArgs Collector = new()
    {
        Enabled = true,
        Endpoint = "http://collector.observability.svc.cluster.local:4318/v1/metrics",
        AllowedHost = "collector.observability.svc.cluster.local",
        Service = "collector",
        Namespace = "observability",
        App = "collector-pods",
        Environment = "test",
        BackendPort = 4318
    };
}
