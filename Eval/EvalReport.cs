// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cratis.Prompter.Eval;

/// <summary>
/// Aggregates per-question results into the headline metrics - in-scope citation-hit rate, out-of-scope
/// refusal accuracy, and mean groundedness - and renders the markdown and JSON reports.
/// </summary>
/// <param name="results">The scored per-question results.</param>
/// <param name="generatedAt">When the run was produced.</param>
internal sealed class EvalReport(IReadOnlyList<QuestionResult> results, DateTimeOffset generatedAt)
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    readonly QuestionResult[] _inScope = [.. results.Where(result => !result.ExpectedRefusal)];
    readonly QuestionResult[] _outOfScope = [.. results.Where(result => result.ExpectedRefusal)];

    /// <summary>Gets when the run was produced.</summary>
    public DateTimeOffset GeneratedAt => generatedAt;

    int Passed => results.Count(result => result.Passed);

    int CitationHits => _inScope.Count(result => result.CitationHit == true);

    int InScopeAnswered => _inScope.Count(result => !result.ActualRefusal);

    int CorrectRefusals => _outOfScope.Count(result => result.RefusalCorrect);

    int GroundedCount => _inScope.Count(result => result.Groundedness is double score && score >= QuestionResult.GroundednessPassThreshold);

    int Errors => results.Count(result => result.Error is not null);

    double MeanGroundedness
    {
        get
        {
            var scores = _inScope.Where(result => result.Groundedness is not null).Select(result => result.Groundedness!.Value).ToArray();
            return scores.Length == 0 ? 0.0 : scores.Average();
        }
    }

    /// <summary>
    /// Renders the full markdown report - the summary table followed by a per-question table.
    /// </summary>
    /// <returns>The markdown document.</returns>
    public string ToMarkdown()
    {
        var builder = new StringBuilder();

        builder
            .AppendLine("# Prompter evaluation report")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Generated {generatedAt.ToString("u", CultureInfo.InvariantCulture)}.")
            .AppendLine()
            .AppendLine("## Summary")
            .AppendLine()
            .AppendLine("| Metric | Value |")
            .AppendLine("| --- | --- |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Questions | {results.Count} |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Overall pass rate | {Passed}/{results.Count} ({Pct(Rate(Passed, results.Count))}) |")
            .AppendLine(CultureInfo.InvariantCulture, $"| In-scope citation-hit rate | {CitationHits}/{_inScope.Length} ({Pct(Rate(CitationHits, _inScope.Length))}) |")
            .AppendLine(CultureInfo.InvariantCulture, $"| In-scope answered (not refused) | {InScopeAnswered}/{_inScope.Length} ({Pct(Rate(InScopeAnswered, _inScope.Length))}) |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Out-of-scope refusal accuracy | {CorrectRefusals}/{_outOfScope.Length} ({Pct(Rate(CorrectRefusals, _outOfScope.Length))}) |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Mean groundedness (in-scope, of 5) | {Num(MeanGroundedness)} |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Grounded (>= {Num(QuestionResult.GroundednessPassThreshold)}) | {GroundedCount}/{_inScope.Length} ({Pct(Rate(GroundedCount, _inScope.Length))}) |")
            .AppendLine(CultureInfo.InvariantCulture, $"| Errors | {Errors} |")
            .AppendLine()
            .AppendLine("## Per-question")
            .AppendLine()
            .AppendLine("| Id | Product | Type | Expected | Refusal | Citation | Grounded | Pass | Notes |")
            .AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (var result in results)
        {
            var expected = result.ExpectedRefusal ? "refuse" : "answer";
            var refusal = result.ActualRefusal ? "refused" : "answered";
            var citation = result.CitationHit switch { true => "hit", false => "miss", null => "-" };
            var grounded = result.Groundedness is double score ? score.ToString("0.#", CultureInfo.InvariantCulture) : "-";
            var pass = result.Passed ? "pass" : "fail";
            var notes = result.Error is null ? string.Empty : $"error: {result.Error}";

            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {result.Id} | {result.Product} | {result.Type} | {expected} | {refusal} | {citation} | {grounded} | {pass} | {notes} |");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders the machine-readable JSON report - the same aggregate metrics plus every per-question result.
    /// </summary>
    /// <returns>The JSON document.</returns>
    public string ToJson()
    {
        var document = new
        {
            GeneratedAt = generatedAt,
            Summary = new
            {
                Questions = results.Count,
                Passed,
                PassRate = Rate(Passed, results.Count),
                InScope = _inScope.Length,
                OutOfScope = _outOfScope.Length,
                CitationHits,
                CitationHitRate = Rate(CitationHits, _inScope.Length),
                InScopeAnswered,
                AnswerRate = Rate(InScopeAnswered, _inScope.Length),
                CorrectRefusals,
                RefusalAccuracy = Rate(CorrectRefusals, _outOfScope.Length),
                MeanGroundedness,
                GroundedCount,
                GroundednessThreshold = QuestionResult.GroundednessPassThreshold,
                Errors,
            },
            Questions = results,
        };

        return JsonSerializer.Serialize(document, _jsonOptions);
    }

    /// <summary>
    /// Renders a short human-readable summary of the headline metrics for the console.
    /// </summary>
    /// <returns>The console summary.</returns>
    public string ConsoleSummary()
    {
        return new StringBuilder()
            .AppendLine("Evaluation summary:")
            .AppendLine(CultureInfo.InvariantCulture, $"  Overall pass:        {Passed}/{results.Count} ({Pct(Rate(Passed, results.Count))})")
            .AppendLine(CultureInfo.InvariantCulture, $"  Citation-hit rate:   {CitationHits}/{_inScope.Length} ({Pct(Rate(CitationHits, _inScope.Length))})")
            .AppendLine(CultureInfo.InvariantCulture, $"  Refusal accuracy:    {CorrectRefusals}/{_outOfScope.Length} ({Pct(Rate(CorrectRefusals, _outOfScope.Length))})")
            .AppendLine(CultureInfo.InvariantCulture, $"  Mean groundedness:   {Num(MeanGroundedness)} of 5")
            .Append(CultureInfo.InvariantCulture, $"  Errors:              {Errors}")
            .ToString();
    }

    static double Rate(int numerator, int denominator) => denominator == 0 ? 0.0 : (double)numerator / denominator;

    static string Pct(double fraction) => (fraction * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    static string Num(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
