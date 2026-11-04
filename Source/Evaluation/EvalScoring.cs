// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Evaluation;

/// <summary>Scores observable grounding and required facts without a judge model.</summary>
public static class EvalScoring
{
    /// <summary>The longest answer text, marker included, a result carries.</summary>
    public const int MaxAnswerTextLength = 4000;

    /// <summary>The marker appended to an answer text that was cut.</summary>
    public const string TruncationMarker = " [truncated]";

    /// <summary>Gets the terms no answer may contain: retrieval meta-talk such as "the excerpts do not say".</summary>
    public static readonly IReadOnlyList<string> ForbiddenTerms = RetrievalMetaTalk.Terms;

    /// <summary>Scores a case against a documentation answer.</summary>
    /// <param name="item">The expected behavior.</param>
    /// <param name="answer">The pipeline result.</param>
    /// <param name="cost">The lease spend.</param>
    /// <returns>The result, carrying the bounded answer text.</returns>
    public static EvalResult Score(EvalCase item, Answer answer, decimal cost = 0)
    {
        var expected = item.ExpectedPages.Select(EvalPages.Normalize).ToHashSet(StringComparer.Ordinal);
        var retrieval = answer.Passages.Any(passage => expected.Contains(EvalPages.Normalize(passage.Page.Value)));
        var citation = answer.Citations.Any(page => expected.Contains(EvalPages.Normalize(page.Value)));
        var expectsAnswer = item.Expected == "answer";
        var refusal = answer.IsRefusal == !expectsAnswer;
        string[] misses =
        [
            .. item.MustMention.Where(term => !RetrievalMetaTalk.Mentions(answer.Text, term)),
            .. ForbiddenTerms.Concat(item.MustNotMention).Where(term => RetrievalMetaTalk.Mentions(answer.Text, term)).Select(term => $"!{term}")
        ];
        var terms = misses.Length == 0;
        var passed = refusal && (!expectsAnswer || (citation && terms));
        IReadOnlyList<string> cited = [.. answer.Citations.Select(page => page.Value)];
        return new(item.Id, expectsAnswer, retrieval, citation, refusal, terms, passed, cost, cited, TermMisses: misses, AnswerText: Bounded(answer.Text), Category: answer.Category.ToString(), IsRefusal: answer.IsRefusal);
    }

    static string Bounded(string text)
    {
        if (text.Length <= MaxAnswerTextLength) return text;
        var keep = MaxAnswerTextLength - TruncationMarker.Length;
        if (char.IsHighSurrogate(text[keep - 1])) keep--;
        return text[..keep] + TruncationMarker;
    }
}
