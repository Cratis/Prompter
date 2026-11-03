// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Evaluation;

/// <summary>Scores observable grounding and required facts without a judge model.</summary>
public static class EvalScoring
{
    /// <summary>Gets the terms no answer may contain: retrieval meta-talk such as "the excerpts do not say".</summary>
    public static readonly IReadOnlyList<string> ForbiddenTerms = ["excerpt"];

    /// <summary>Scores a case against a documentation answer.</summary>
    /// <param name="item">The expected behavior.</param>
    /// <param name="answer">The pipeline result.</param>
    /// <param name="cost">The lease spend.</param>
    /// <returns>A text-free result.</returns>
    public static EvalResult Score(EvalCase item, Answer answer, decimal cost = 0)
    {
        var expected = item.ExpectedPages.Select(EvalPages.Normalize).ToHashSet(StringComparer.Ordinal);
        var retrieval = answer.Passages.Any(passage => expected.Contains(EvalPages.Normalize(passage.Page.Value)));
        var citation = answer.Citations.Any(page => expected.Contains(EvalPages.Normalize(page.Value)));
        var expectsAnswer = item.Expected == "answer";
        var refusal = answer.IsRefusal == !expectsAnswer;
        string[] misses =
        [
            .. item.MustMention.Where(term => !Mentions(answer, term)),
            .. ForbiddenTerms.Concat(item.MustNotMention).Where(term => Mentions(answer, term)).Select(term => $"!{term}")
        ];
        var terms = misses.Length == 0;
        var passed = refusal && (!expectsAnswer || (citation && terms));
        return new(item.Id, expectsAnswer, retrieval, citation, refusal, terms, passed, cost, [.. answer.Citations.Select(page => page.Value)], TermMisses: misses);
    }

    /// <summary>Checks a mention where hyphens and whitespace are equivalent, so "event-sourcing" mentions "event sourcing".</summary>
    /// <param name="answer">The answer to search.</param>
    /// <param name="term">The term to find, ignoring case.</param>
    /// <returns><see langword="true"/> when the answer mentions the term.</returns>
    static bool Mentions(Answer answer, string term) => Words(answer.Text).Contains(Words(term), StringComparison.OrdinalIgnoreCase);

    static string Words(string text) => string.Join(' ', text.Split([' ', '-', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
