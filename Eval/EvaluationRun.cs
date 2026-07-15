// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Eval.Scoring;
using Microsoft.Extensions.AI.Evaluation;

namespace Cratis.Prompter.Eval;

/// <summary>
/// Runs a single golden question through the bot's <see cref="IAnswers"/> pipeline and scores the three
/// dimensions: citation-hit and refusal-accuracy (deterministic), and groundedness (LLM judge).
/// </summary>
/// <param name="answers">The bot's answering pipeline.</param>
/// <param name="evaluator">The groundedness judge.</param>
/// <param name="chatConfiguration">The chat configuration wrapping the judge's chat client.</param>
internal sealed class EvaluationRun(IAnswers answers, GroundednessEvaluator evaluator, ChatConfiguration chatConfiguration)
{
    /// <summary>
    /// Answers a golden question and scores it. Any failure is captured on the result so one bad question
    /// never aborts the whole run.
    /// </summary>
    /// <param name="question">The golden question to score.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The scored <see cref="QuestionResult"/>.</returns>
    public async Task<QuestionResult> Score(GoldenQuestion question, CancellationToken cancellationToken = default)
    {
        try
        {
            var answer = await answers.For(new Question(question.Question), "eval", "eval", cancellationToken);
            var refusalCorrect = RefusalAccuracy.IsCorrect(question.ExpectsRefusal, answer.IsRefusal);

            bool? citationHit = question.ExpectsRefusal
                ? null
                : CitationHit.IsHit(question.ExpectedPages, CandidatePages(answer));

            double? groundedness = null;
            if (!question.ExpectsRefusal && !answer.IsRefusal)
            {
                groundedness = await Judge(question, answer, cancellationToken);
            }

            return new QuestionResult(
                question.Id,
                question.Product,
                question.Type,
                question.ExpectsRefusal,
                answer.IsRefusal,
                refusalCorrect,
                citationHit,
                groundedness,
                answer.Confidence,
                [.. answer.Citations.Select(citation => citation.Value)],
                Error: null);
        }
        catch (Exception exception)
        {
            return new QuestionResult(
                question.Id,
                question.Product,
                question.Type,
                question.ExpectsRefusal,
                ActualRefusal: false,
                RefusalCorrect: false,
                CitationHit: null,
                Groundedness: null,
                Confidence: 0,
                Citations: [],
                Error: exception.Message);
        }
    }

    static IEnumerable<string> CandidatePages(Answer answer) =>
        answer.Citations
            .Select(citation => citation.Value)
            .Concat(answer.Passages.Select(passage => passage.Page.Value));

    async Task<double?> Judge(GoldenQuestion question, Answer answer, CancellationToken cancellationToken)
    {
        var grounding = string.Join(
            Environment.NewLine + Environment.NewLine,
            answer.Passages.Select(passage => $"[{passage.Page.Value}] {passage.HeadingPath}{Environment.NewLine}{passage.Content}"));

        var evaluation = await evaluator.EvaluateAsync(
            question.Question,
            answer.Text,
            chatConfiguration,
            [new GroundednessContext(grounding)],
            cancellationToken);

        return evaluation.TryGet<NumericMetric>(GroundednessEvaluator.MetricName, out var metric) ? metric.Value : null;
    }
}
