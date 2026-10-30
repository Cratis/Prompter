// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Routing;

/// <summary>Maps strictly validated wire distributions to advisory branches.</summary>
internal static class DecisionResponse
{
    /// <summary>Rejects malformed or uncertain responses before selecting finite branches.</summary>
    /// <param name="root">The untrusted response.</param>
    /// <param name="expected">Ordered request decisions.</param>
    /// <param name="options">Unvalidated confidence defaults.</param>
    /// <param name="baseline">Safe deterministic fallback.</param>
    /// <returns>Validated advisory data or fallback.</returns>
    public static Classification Parse(JsonElement root, IReadOnlyList<DecisionChoices> expected, RoutingOptions options, Classification baseline)
    {
        var distributions = ReadDistributions(root, expected);
        if (distributions is null) return baseline with { Fallback = ClassificationFallback.InvalidResponse };
        if (!ThresholdsAreValid(options) || distributions.Any(distribution => IsUncertain(distribution, options)))
        {
            return baseline with { Fallback = ClassificationFallback.LowConfidence, Confidence = ConfidenceBucket.Low };
        }
        var product = distributions[0].Winner;
        var intent = Enum.Parse<ClassificationIntent>(distributions[1].Winner, true);
        var action = SafeAction(distributions[2].Winner, product, intent);
        return new(new(product), intent, action, ConfidenceBucket.High, ClassificationSource.Laya, ClassificationFallback.None);
    }

    static ClassificationAction SafeAction(string winner, string product, ClassificationIntent intent)
    {
        if (product == "unknown") return ClassificationAction.Clarify;
        if (winner == "answer") return ClassificationAction.Answer;
        var issueIntent = intent is ClassificationIntent.Bug or ClassificationIntent.Feature or ClassificationIntent.Idea;
        return winner == "offer-issue" && issueIntent ? ClassificationAction.OfferIssue : ClassificationAction.Clarify;
    }

    static DecisionDistribution[]? ReadDistributions(JsonElement root, IReadOnlyList<DecisionChoices> expected)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        if (root.EnumerateObject().Count(property => property.Name == "results") != 1) return null;
        var results = root.GetProperty("results");
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != expected.Count) return null;
        var distributions = results.EnumerateArray().Select((result, index) => DecisionDistribution.Parse(result, expected[index])).ToArray();
        return distributions.Any(distribution => distribution is null) ? null : distributions.Select(distribution => distribution!).ToArray();
    }

    static bool IsUncertain(DecisionDistribution distribution, RoutingOptions options) => distribution.Probability < options.MinimumConfidence
        || distribution.Margin <= 0 || distribution.Margin < options.MinimumMargin;

    static bool ThresholdsAreValid(RoutingOptions options) => double.IsFinite(options.MinimumConfidence)
        && options.MinimumConfidence is >= 0.5 and <= 1 && double.IsFinite(options.MinimumMargin) && options.MinimumMargin is >= 0 and <= 1;
}
