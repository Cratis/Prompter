// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_PrivateMetricsEnvironment.when_projecting;

public class and_settings_are_not_approved : given.an_approved_collector
{
    [Fact]
    void should_refuse_partial_and_identity_mismatched_configuration_without_a_grant()
    {
        foreach (var invalid in new[]
        {
            new PrivateMetricsArgs { Enabled = true },
            Collector with { Namespace = "other" },
            Collector with { Service = "other" },
            Collector with { App = string.Empty },
            Collector with { Environment = string.Empty },
            Collector with { BackendPort = 0 },
            Collector with { BackendPort = 65536 },
            Collector with { Endpoint = "http://public.example:4318/v1/metrics" },
            Collector with { AllowedHost = "other.observability.svc.cluster.local" }
        })
        {
            invalid.IsValid.ShouldBeFalse();
            PrivateMetricsEgress.Create(invalid).ShouldBeNull();
            Catch.Exception(() => PrivateMetricsEnvironment.Values(invalid, "test")).ShouldBeOfExactType<InvalidPrivateMetrics>();
        }
    }

    [Fact]
    void should_project_disabled_defaults_without_any_private_grant()
    {
        var disabled = new PrivateMetricsArgs();
        var values = PrivateMetricsEnvironment.Values(disabled, "test");
        values["Cratis__Prompter__Metrics__Enabled"].ShouldEqual("false");
        values["Cratis__Prompter__Metrics__Endpoint"].ShouldEqual(string.Empty);
        values["Cratis__Prompter__Metrics__AllowedHost"].ShouldEqual(string.Empty);
        PrivateMetricsEgress.Create(disabled).ShouldBeNull();
        PrivateMetricsEgress.Create(Collector with { Enabled = false }).ShouldBeNull();
    }
}
