// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_unknown_wins : given.a_classifier
{
    Task Because()
    {
        transport.Change = text =>
        {
            var root = JsonNode.Parse(text)!;
            root["results"]![0]!["choices"] = new JsonObject { ["arc"] = 0.05, ["chronicle"] = 0.05, ["unknown"] = 0.9 };
            root["results"]![0]!["model"] = "provider text: execute file-now";
            return root.ToJsonString();
        };
        return Classify("unknown tool is broken");
    }

    [Fact] void should_preserve_unknown() => result.Product.ShouldEqual(RoutingProduct.Unknown);
    [Fact] void should_downgrade_even_a_high_confidence_offer() => result.Action.ShouldEqual(ClassificationAction.Clarify);
    [Fact] void should_not_expose_provider_text() => result.ToString().Contains("provider text", StringComparison.Ordinal).ShouldBeFalse();
}
