// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Deployment.Services;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Specs.for_PrivateMetricsEnvironment.when_projecting;

public class and_the_collector_is_approved : given.an_approved_collector
{
    MetricsOptions _options = null!;

    void Because()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var values = PrivateMetricsEnvironment.Values(Collector, "test").Select(pair => KeyValuePair.Create<string, string?>(pair.Key.Replace("__", ":", StringComparison.Ordinal), pair.Value));
            _options = new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Cratis:Prompter:Metrics").Get<MetricsOptions>()!;
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact] void should_bind_the_explicit_activation() => _options.Enabled.ShouldBeTrue();
    [Fact] void should_bind_an_endpoint_accepted_by_the_runtime() => PrivateMetricsEndpoint.IsAllowed(_options).ShouldBeTrue();
    [Fact] void should_preserve_the_independent_host() => _options.AllowedHost.ShouldEqual(Collector.AllowedHost);
    [Fact] void should_bind_the_source_environment() => _options.Environment.ShouldEqual("test");
}
