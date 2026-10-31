// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Routing;

/// <summary>A fully validated normalized distribution winner.</summary>
/// <param name="Winner">An expected choice key.</param>
/// <param name="Probability">The finite winning probability.</param>
/// <param name="Margin">The difference from the next choice.</param>
internal sealed record DecisionDistribution(string Winner, double Probability, double Margin)
{
    /// <summary>Validates correlation, exact choice set and normalized finite probabilities.</summary>
    /// <param name="result">An untrusted wire result.</param>
    /// <param name="expected">The request's correlation and choices.</param>
    /// <returns>A validated distribution, or null.</returns>
    public static DecisionDistribution? Parse(JsonElement result, DecisionChoices expected)
    {
        if (!IsCorrelated(result, expected.RequestId)) return null;
        if (!result.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Object) return null;
        var properties = choices.EnumerateObject().ToArray();
        if (!HasExpectedKeys(properties, expected.Keys)) return null;
        var scores = new List<KeyValuePair<string, double>>();
        foreach (var property in properties)
        {
            var probability = ReadProbability(property.Value);
            if (probability is null) return null;
            scores.Add(new(property.Name, probability.Value));
        }
        return RankNormalized(scores);
    }

    static bool IsCorrelated(JsonElement result, string expected)
    {
        if (result.ValueKind != JsonValueKind.Object) return false;
        var names = result.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) return false;
        return result.TryGetProperty("requestId", out var correlation)
            && correlation.ValueKind == JsonValueKind.String && correlation.GetString() == expected;
    }

    static bool HasExpectedKeys(JsonProperty[] properties, IReadOnlyList<string> expected)
    {
        var keys = properties.Select(property => property.Name).ToArray();
        return keys.Length == expected.Count && keys.Distinct(StringComparer.Ordinal).Count() == keys.Length
            && !keys.Except(expected, StringComparer.Ordinal).Any();
    }

    static double? ReadProbability(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var probability)) return null;
        if (!double.IsFinite(probability)) return null;
        return probability is >= 0 and <= 1 ? probability : null;
    }

    static DecisionDistribution? RankNormalized(List<KeyValuePair<string, double>> scores)
    {
        if (Math.Abs(scores.Sum(score => score.Value) - 1) > 0.001) return null;
        var ranked = scores.OrderByDescending(score => score.Value).ThenBy(score => score.Key, StringComparer.Ordinal).ToArray();
        return new(ranked[0].Key, ranked[0].Value, ranked.Length > 1 ? ranked[0].Value - ranked[1].Value : 1);
    }
}
