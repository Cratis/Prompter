// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Cli;

/// <summary>Explicit opt-in and bounded quality thresholds for paid evaluation.</summary>
/// <param name="Paid">Whether paid execution was explicitly requested.</param>
/// <param name="Set">The YAML path.</param>
/// <param name="Case">An optional case identifier.</param>
/// <param name="Limit">An optional maximum case count.</param>
/// <param name="MinPassRate">The required pass rate.</param>
/// <param name="MinRetrievalHitRate">The required answer retrieval-hit rate.</param>
/// <param name="MinRefusalAccuracy">The required refusal accuracy.</param>
/// <param name="MaxCost">The dollar cap, checked between cases.</param>
/// <param name="Out">The optional JSON output path.</param>
public sealed record EvalArguments(bool Paid, string? Set = null, string? Case = null, int? Limit = null,
    double MinPassRate = 0.8, double MinRetrievalHitRate = 0.8, double MinRefusalAccuracy = 0.9,
    decimal MaxCost = 2.00m, string? Out = null)
{
    /// <summary>Gets usage without any user-supplied values.</summary>
    public const string Usage = "Usage: eval --paid [--set path] [--case id] [--limit count] [--min-pass-rate 0..1] [--min-retrieval-hit-rate 0..1] [--min-refusal-accuracy 0..1] [--max-cost dollars] [--out path]";

    /// <summary>Parses only known flags; malformed input returns no arguments.</summary>
    /// <param name="tokens">Tokens after the mode.</param>
    /// <returns>The arguments, or null for a usage error.</returns>
    public static EvalArguments? Parse(IEnumerable<string> tokens)
    {
        var all = tokens.ToArray();
        var result = new EvalArguments(false);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < all.Length; index++)
        {
            var flag = all[index];
            if (!seen.Add(flag)) return null;
            if (flag == "--paid")
            {
                result = result with { Paid = true };
                continue;
            }

            if (++index == all.Length || all[index].StartsWith("--", StringComparison.Ordinal)) return null;
            if (Apply(result, flag, all[index]) is not { } parsed) return null;
            result = parsed;
        }

        return result;
    }

    static EvalArguments? Apply(EvalArguments result, string flag, string value) => flag switch
    {
        "--set" => result with { Set = value },
        "--case" => result with { Case = value },
        "--out" => result with { Out = value },
        "--limit" => int.TryParse(value, out var count) && count > 0 ? result with { Limit = count } : null,
        "--max-cost" => Cost(value) is { } cost ? result with { MaxCost = cost } : null,
        _ => ApplyRate(result, flag, value)
    };

    static EvalArguments? ApplyRate(EvalArguments result, string flag, string value)
    {
        if (Rate(value) is not { } rate) return null;
        return flag switch
        {
            "--min-pass-rate" => result with { MinPassRate = rate },
            "--min-retrieval-hit-rate" => result with { MinRetrievalHitRate = rate },
            "--min-refusal-accuracy" => result with { MinRefusalAccuracy = rate },
            _ => null
        };
    }

    static decimal? Cost(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) && cost > 0 ? cost : null;

    static double? Rate(string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) &&
        double.IsFinite(rate) && rate is >= 0 and <= 1 ? rate : null;
}
