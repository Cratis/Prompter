// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_registering;

public class and_the_boundary_is_composed : Specification
{
    ServiceProvider _provider = null!;
    IRequestClassifier _classifier = null!;
    Classification _result = null!;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddRequestRouting(new RoutingOptions(), ["arc", "chronicle"]);
        _provider = services.BuildServiceProvider();
    }

    async Task Because()
    {
        _classifier = _provider.GetRequiredService<IRequestClassifier>();
        _result = await _classifier.Classify("Arc question", []);
    }

    async Task Destroy() => await _provider.DisposeAsync();

    [Fact] void should_share_one_concurrency_boundary() => ReferenceEquals(_classifier, _provider.GetRequiredService<IRequestClassifier>()).ShouldBeTrue();
    [Fact] void should_register_a_no_op_sink() => _provider.GetRequiredService<IClassificationSink>().ShouldBeOfExactType<NoOpClassificationSink>();
    [Fact] void should_not_need_a_live_service() => _result.Fallback.ShouldEqual(ClassificationFallback.NotConfigured);
}
