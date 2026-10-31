// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_PrivateMetricsEndpoint.when_validating;

public class and_the_endpoint_is_not_approved : Specification
{
    [Fact]
    void should_reject_hostile_destinations_and_paths()
    {
        const string host = "collector.observability.svc.cluster.local";
        foreach (var endpoint in new[]
        {
            "http://127.0.0.1:4318/v1/metrics", "http://169.254.169.254:4318/v1/metrics",
            "http://10.0.0.1:4318/v1/metrics", "http://localhost:4318/v1/metrics",
            "http://public.example:4318/v1/metrics", $"http://{host}.example:4318/v1/metrics",
            $"http://{host}:80/v1/metrics", $"https://{host}:4318/v1/metrics",
            $"http://{host}:4318/v1/metrics?x=1", $"http://{host}:4318/v1/metrics#x",
            $"http://user@{host}:4318/v1/metrics", $"http://{host}:4318/v1/metrics/extra",
            $"http://{host}:4318/extra/../v1/metrics", $"http://{host}:4318/v1/traces"
        })
        {
            PrivateMetricsEndpoint.IsAllowed(new() { Endpoint = new(endpoint), AllowedHost = host }).ShouldBeFalse();
        }
    }

    [Fact]
    void should_require_independent_host_approval() => PrivateMetricsEndpoint.IsAllowed(new()
    {
        Endpoint = new("http://collector.observability.svc.cluster.local:4318/v1/metrics")
    }).ShouldBeFalse();

    [Fact]
    void should_fail_closed_without_echoing_the_destination()
    {
        var error = Catch.Exception(() =>
        {
            using var export = new MetricsExport(Options.Create(new MetricsOptions
            {
                Enabled = true,
                Endpoint = new("http://public.example:4318/v1/metrics"),
                AllowedHost = "public.example"
            }));
        });
        error.ShouldBeOfExactType<OptionsValidationException>();
        error!.Message.ShouldEqual("Private metrics endpoint is not approved.");
    }

    [Fact]
    void should_construct_no_exporter_for_disabled_hostile_settings()
    {
        using var export = new MetricsExport(Options.Create(new MetricsOptions { Endpoint = new("http://public.example") }));
    }
}
