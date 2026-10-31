// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_routing_configuration_is_invalid : given.a_classifier
{
    readonly List<Classification> _results = [];

    async Task Because()
    {
        options.Mode = (ClassificationMode)2;
        await Classify();
        _results.Add(result);
        options.Mode = ClassificationMode.Active;
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.1, 1.1 })
        {
            options.MinimumConfidence = value;
            await Classify();
            _results.Add(result);
            options.MinimumConfidence = 0.75;
            options.MinimumMargin = value;
            await Classify();
            _results.Add(result);
            options.MinimumMargin = 0.15;
        }
    }

    [Fact] void should_reject_invalid_mode_and_thresholds() => _results.TrueForAll(value => value.Fallback == ClassificationFallback.EndpointRejected).ShouldBeTrue();
    [Fact] void should_make_no_requests() => transport.Calls.ShouldEqual(0);
}
