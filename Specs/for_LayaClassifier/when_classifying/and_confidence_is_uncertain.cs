// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_confidence_is_uncertain : given.a_classifier
{
    readonly List<Classification> _results = [];

    async Task Because()
    {
        foreach (var probability in new[] { 0.5, 0.7, 0.8 })
        {
            options.MinimumMargin = probability == 0.8 ? 0.75 : 0.15;
            transport.Change = text =>
            {
                var root = JsonNode.Parse(text)!;
                root["results"]![0]!["choices"] = new JsonObject { ["arc"] = probability, ["chronicle"] = 1 - probability, ["unknown"] = 0 };
                return root.ToJsonString();
            };
            await Classify("Arc and Chronicle both fail");
            _results.Add(result);
        }
    }

    [Fact] void should_reject_ties_low_probability_and_small_margin() => _results.TrueForAll(value => value.Fallback == ClassificationFallback.LowConfidence).ShouldBeTrue();
    [Fact] void should_not_guess_between_products() => _results.TrueForAll(value => value.Product == RoutingProduct.Unknown).ShouldBeTrue();
    [Fact] void should_never_offer_filing() => _results.TrueForAll(value => value.Action == ClassificationAction.Clarify).ShouldBeTrue();
}
