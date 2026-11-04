// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Evaluation;

/// <summary>A quality result safe to include in evaluation reports; the answer text is bounded and only for diagnosing failures.</summary>
/// <param name="Id">The case identifier.</param>
/// <param name="ExpectsAnswer">Whether retrieval is expected.</param>
/// <param name="RetrievalHit">Whether an expected page was retrieved.</param>
/// <param name="CitationHit">Whether an expected page was cited.</param>
/// <param name="RefusalCorrect">Whether refusal matched the expected outcome.</param>
/// <param name="MustMentionHit">Whether all required terms were present.</param>
/// <param name="Passed">Whether the case passed.</param>
/// <param name="Cost">Known spend plus conservative unknown exposure.</param>
/// <param name="CitedPages">Citations, not generated answer text.</param>
/// <param name="Reason">A fixed failure reason, if applicable.</param>
/// <param name="TermMisses">The case's own configured terms that failed: a required term the answer lacks, or a forbidden term prefixed with an exclamation mark that it contains. Never generated text.</param>
/// <param name="AnswerText">The answer text, truncated to <see cref="EvalScoring.MaxAnswerTextLength"/> characters with a marker when cut.</param>
/// <param name="Category">The model's triage category for the answer.</param>
/// <param name="IsRefusal">Whether the answer was a refusal.</param>
public sealed record EvalResult(string Id, bool ExpectsAnswer, bool RetrievalHit, bool CitationHit,
    bool RefusalCorrect, bool MustMentionHit, bool Passed, decimal Cost, IReadOnlyList<string> CitedPages, string? Reason = null,
    IReadOnlyList<string>? TermMisses = null, string? AnswerText = null, string? Category = null, bool IsRefusal = false);
