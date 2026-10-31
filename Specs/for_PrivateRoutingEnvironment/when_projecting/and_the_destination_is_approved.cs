// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Deployment.Services;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Specs.for_PrivateRoutingEnvironment.when_projecting;

public class and_the_destination_is_approved : given.an_approved_destination
{
    RoutingOptions _options = null!;

    void Because()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var values = PrivateRoutingEnvironment.Values(Destination).Select(pair => KeyValuePair.Create<string, string?>(pair.Key.Replace("__", ":", StringComparison.Ordinal), pair.Value));
            _options = new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Cratis:Prompter:Routing").Get<RoutingOptions>()!;
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact] void should_bind_the_exact_approved_origin() => _options.Endpoint!.OriginalString.ShouldEqual(Destination.Endpoint);
    [Fact] void should_preserve_independent_host_approval() => _options.AllowedHost.ShouldEqual(Destination.AllowedHost);
    [Fact]
    async Task should_accept_the_runtime_origin()
    {
        using var transport = new for_LayaClassifier.given.a_classifier.fake_decisions();
        using var client = new HttpClient(transport);
        var classifier = new LayaClassifier(client, _options, ["arc"], new NoOpClassificationSink());
        await classifier.Classify("Arc is broken", [], TestContext.Current.CancellationToken);
        transport.Calls.ShouldEqual(1);
    }

    [Fact] void should_preserve_explicit_mode() => _options.Mode.ShouldEqual(ClassificationMode.Active);
    [Fact] void should_leave_shadow_sampling_off() => _options.ShadowPercentage.ShouldEqual(0);
    [Fact] void should_preserve_confidence() => _options.MinimumConfidence.ShouldEqual(0.75);
    [Fact] void should_preserve_margin() => _options.MinimumMargin.ShouldEqual(0.15);
}
