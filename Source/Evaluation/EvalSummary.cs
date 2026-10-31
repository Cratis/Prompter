// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Evaluation;

/// <summary>Aggregate quality and spend without answer or question text.</summary>
/// <param name="Results">The case results.</param>
/// <param name="PassRate">The fraction passing all case rules.</param>
/// <param name="RetrievalHitRate">The fraction of answer cases retrieving an expected page.</param>
/// <param name="RefusalAccuracy">The fraction with correct refusal behavior.</param>
/// <param name="TotalCost">The total charged or conservatively reserved dollars.</param>
/// <param name="CostCapStopped">Whether the cap stopped case selection.</param>
/// <param name="ExitCode">Zero for success, one for quality or cap failure.</param>
public sealed record EvalSummary(IReadOnlyList<EvalResult> Results, double PassRate, double RetrievalHitRate,
    double RefusalAccuracy, decimal TotalCost, bool CostCapStopped, int ExitCode)
{
    /// <summary>Builds aggregate rates and applies every configured threshold.</summary>
    /// <param name="results">The scored cases.</param>
    /// <param name="arguments">The required quality thresholds.</param>
    /// <param name="costCapStopped">Whether not all selected cases ran.</param>
    /// <returns>The text-free summary.</returns>
    public static EvalSummary From(IReadOnlyList<EvalResult> results, EvalArguments arguments, bool costCapStopped = false)
    {
        var answers = results.Where(result => result.ExpectsAnswer).ToArray();
        var pass = Rate(results.Count(result => result.Passed), results.Count);
        var retrieval = answers.Length == 0 ? 1 : Rate(answers.Count(result => result.RetrievalHit), answers.Length);
        var refusal = Rate(results.Count(result => result.RefusalCorrect), results.Count);
        var cost = results.Sum(result => result.Cost);
        var success = results.Count > 0 && !costCapStopped && cost <= arguments.MaxCost &&
            pass >= arguments.MinPassRate && retrieval >= arguments.MinRetrievalHitRate && refusal >= arguments.MinRefusalAccuracy;
        return new(results, pass, retrieval, refusal, cost, costCapStopped, success ? 0 : 1);
    }

    /// <summary>Formats aggregate results only.</summary>
    /// <returns>Console lines without raw input or generated text.</returns>
    public IEnumerable<string> Lines()
    {
        yield return string.Create(CultureInfo.InvariantCulture, $"Eval: {Results.Count} cases; pass {PassRate:P1}; retrieval {RetrievalHitRate:P1}; refusal {RefusalAccuracy:P1}; cost ${TotalCost:F4}");
        if (CostCapStopped) yield return "Stopped at the configured cost cap.";
    }

    static double Rate(int hits, int total) => total == 0 ? 0 : (double)hits / total;
}
