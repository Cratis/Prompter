// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Eval;

/// <summary>Formats a scored question without changing its scoring decisions.</summary>
internal static class QuestionConsoleLine
{
    /// <summary>Renders the compact console summary.</summary>
    /// <param name="result">The scored question.</param>
    /// <returns>The console line.</returns>
    public static string Format(QuestionResult result)
    {
        var verdict = result.Passed ? "PASS" : "FAIL";

        if (result.Error is not null)
        {
            return $"  [{result.Id}] {verdict}  error: {result.Error}";
        }

        return $"  [{result.Id}] {verdict}  {CitationStatus(result)} {RefusalStatus(result)} {GroundednessStatus(result)}";
    }

    static string CitationStatus(QuestionResult result) => result.CitationHit switch
    {
        true => "cite=hit",
        false => "cite=miss",
        null => "cite=n/a",
    };

    static string RefusalStatus(QuestionResult result) => (result.ExpectedRefusal, result.ActualRefusal) switch
    {
        (true, true) => "refusal=ok",
        (true, false) => "refusal=answered",
        (false, true) => "refusal=refused",
        (false, false) => "refusal=answered",
    };

    static string GroundednessStatus(QuestionResult result) => result.Groundedness is double score
        ? "grounded=" + score.ToString("0.#", CultureInfo.InvariantCulture)
        : "grounded=n/a";
}
