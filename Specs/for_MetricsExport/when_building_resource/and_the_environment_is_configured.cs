// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Specs.for_MetricsExport.when_building_resource;

public class and_the_environment_is_configured : Specification
{
    [Fact]
    void should_export_only_fixed_operational_environments()
    {
        foreach (var environment in new[] { "production", "staging", "development", "test" })
        {
            var attributes = MetricsExport.CreateResource(environment).Build().Attributes.ToDictionary();

            attributes["deployment.environment.name"].ShouldEqual(environment);
            attributes["service.name"].ShouldEqual("prompter");
            attributes.Keys.Order().ShouldEqual(["deployment.environment.name", "service.instance.id", "service.name"]);
        }
    }

    [Fact]
    void should_replace_unrecognized_and_empty_values_with_unknown()
    {
        foreach (var environment in new[] { "private-request-input", string.Empty, "Production" })
        {
            var attributes = MetricsExport.CreateResource(environment).Build().Attributes.ToDictionary();

            attributes["deployment.environment.name"].ShouldEqual("unknown");
        }
    }

    [Fact]
    void should_leave_exporting_disabled_and_the_environment_unknown_by_default()
    {
        var options = new MetricsOptions();

        options.Enabled.ShouldBeFalse();
        options.Environment.ShouldEqual("unknown");
    }
}
