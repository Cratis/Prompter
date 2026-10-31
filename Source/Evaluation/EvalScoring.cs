// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Evaluation;

/// <summary>Scores observable grounding and required facts without a judge model.</summary>
public static class EvalScoring
{
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
        var terms = item.MustMention.All(term => answer.Text.Contains(term, StringComparison.OrdinalIgnoreCase));
        return new(item.Id, expectsAnswer, retrieval, citation, refusal, terms, refusal && (!expectsAnswer || (citation && terms)), cost, [.. answer.Citations.Select(page => page.Value)]);
    }
}
