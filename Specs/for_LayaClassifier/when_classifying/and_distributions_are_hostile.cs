// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_distributions_are_hostile : given.a_classifier
{
    readonly List<Classification> _results = [];

    async Task Because()
    {
        Action<JsonNode>[] corruptions =
        [
            root => root["results"]![0]!["choices"]!.AsObject().Remove("arc"),
            root => root["results"]![0]!["choices"]!["invented-product"] = 0,
            root => root["results"]![1]!["choices"]!["execute"] = 0,
            root => root["results"]![2]!["choices"]!["file-now"] = 0,
            root => root["results"]![0]!["choices"]!["arc"] = -1,
            root => root["results"]![0]!["choices"]!["arc"] = 2,
            root => root["results"]![0]!["choices"]!["arc"] = "NaN",
            root => root["results"]![0]!["choices"]!["arc"] = 0,
            root => root["results"]![0]!["requestId"] = "wrong-correlation",
            root => root["results"]!.AsArray().RemoveAt(0),
            root => root["results"]!.AsArray().Add(root["results"]![0]!.DeepClone()),
            root => root["results"]![1] = root["results"]![0]!.DeepClone(),
            root => root["results"]![0]!["choices"] = null
        ];
        foreach (var corruption in corruptions)
        {
            transport.Change = text =>
            {
                var root = JsonNode.Parse(text)!;
                corruption(root);
                return root.ToJsonString();
            };
            await Classify();
            _results.Add(result);
        }
        Func<string, string>[] wireCorruptions =
        [
            text => text.Replace("\"arc\":0.9", "\"arc\":1e999", StringComparison.Ordinal),
            text => text.Replace("\"arc\":0.9", "\"arc\":0.9,\"arc\":0.9", StringComparison.Ordinal),
            _ => "not-json",
            _ => "[]",
            _ => new string('x', 65537)
        ];
        foreach (var corruption in wireCorruptions)
        {
            transport.Change = corruption;
            await Classify();
            _results.Add(result);
        }
    }

    [Fact] void should_reject_every_malformed_distribution() => _results.TrueForAll(value => value.Fallback == ClassificationFallback.InvalidResponse).ShouldBeTrue();
    [Fact] void should_never_elevate_to_an_offer() => _results.TrueForAll(value => value.Action == ClassificationAction.Clarify).ShouldBeTrue();
    [Fact] void should_make_exactly_one_attempt_per_request() => transport.Calls.ShouldEqual(18);
}
