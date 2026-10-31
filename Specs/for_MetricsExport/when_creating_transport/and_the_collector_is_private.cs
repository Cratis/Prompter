// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Specs.for_MetricsExport.when_creating_transport;

public class and_the_collector_is_private : Specification
{
    [Fact]
    void should_never_delegate_to_a_proxy_or_follow_a_redirect()
    {
        using var transport = MetricsExport.CreateTransport();
        transport.UseProxy.ShouldBeFalse();
        transport.AllowAutoRedirect.ShouldBeFalse();
        transport.ConnectTimeout.ShouldEqual(TimeSpan.FromSeconds(1));
    }

    [Fact]
    void should_bound_the_export_request()
    {
        using var client = MetricsExport.CreateClient();
        client.Timeout.ShouldEqual(TimeSpan.FromSeconds(3));
    }
}
