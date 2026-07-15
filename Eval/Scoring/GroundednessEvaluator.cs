// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// An <see cref="IEvaluator"/> that uses an <see cref="IChatClient"/> judge to score how well an answer is
/// grounded in the retrieved documentation passages. Cribbed from dotnet/eShopSupport's
/// <c>AnswerScoringEvaluator</c>, but scoring groundedness (is the answer supported by the retrieved context,
/// with no fabrication) rather than closeness to a reference answer - the metric that matters for a RAG bot.
/// </summary>
internal sealed class GroundednessEvaluator : IEvaluator
{
    /// <summary>The name of the metric this evaluator produces.</summary>
    public const string MetricName = "Groundedness";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    /// <inheritdoc/>
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelResponse);

        var metric = new NumericMetric(MetricName);
        var result = new EvaluationResult(metric);

        if (chatConfiguration is null)
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("A chat configuration with a judge chat client is required to evaluate groundedness."));
            return result;
        }

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            result.AddDiagnosticsToAllMetrics(EvaluationDiagnostic.Error("The answer supplied for evaluation was empty."));
            return result;
        }

        if (additionalContext?.OfType<GroundednessContext>().FirstOrDefault() is not { } context)
        {
            result.AddDiagnosticsToAllMetrics(
                EvaluationDiagnostic.Error("The grounding context must be supplied to evaluate groundedness."));
            return result;
        }

        messages.TryGetUserRequest(out var userRequest, out _);
        var question = userRequest?.RenderText() ?? string.Empty;
        var answer = modelResponse.RenderText();

        var response = await chatConfiguration.ChatClient.GetResponseAsync<GroundednessRating>(
            [new ChatMessage(ChatRole.User, BuildPrompt(question, answer, context.GroundingContext))],
            options: null,
            useJsonSchemaResponseFormat: false,
            cancellationToken);

        if (!response.TryGetResult(out var rating) || rating is null)
        {
            result.AddDiagnosticsToAllMetrics(EvaluationDiagnostic.Error("The judge did not return a valid groundedness score."));
            return result;
        }

        metric.Value = rating.Score;

        if (!string.IsNullOrWhiteSpace(rating.Reason))
        {
            metric.AddDiagnostics(EvaluationDiagnostic.Informational(rating.Reason));
        }

        metric.Interpretation = Interpret(metric);
        return result;
    }

    /// <summary>
    /// Maps a groundedness score to a rating, treating a missing, unacceptable, or poor score as a failure.
    /// </summary>
    /// <param name="metric">The metric holding the score.</param>
    /// <returns>The interpretation of the score.</returns>
    internal static EvaluationMetricInterpretation Interpret(NumericMetric metric)
    {
        var score = metric.Value ?? -1.0;
        var rating = score switch
        {
            5.0 => EvaluationRating.Exceptional,
            4.0 => EvaluationRating.Good,
            3.0 => EvaluationRating.Average,
            2.0 => EvaluationRating.Poor,
            1.0 => EvaluationRating.Unacceptable,
            _ => EvaluationRating.Inconclusive,
        };

        var failed = rating is EvaluationRating.Inconclusive or EvaluationRating.Unacceptable or EvaluationRating.Poor;
        return new EvaluationMetricInterpretation(rating, failed: failed);
    }

    static string BuildPrompt(string question, string answer, string groundingContext) =>
        $$"""
        You are evaluating an AI documentation assistant for the Cratis stack. The assistant must answer only
        from the retrieved documentation passages it was given, cite them, and never invent facts.

        You are scoring GROUNDEDNESS: is every claim in the assistant's answer supported by the grounding
        passages below? An answer that adds facts not present in the passages, or contradicts them, is poorly
        grounded even if it sounds plausible. There is no penalty for an answer that is a faithful, well-cited
        summary of the passages.

        <question>{{question}}</question>

        <groundingPassages>
        {{groundingContext}}
        </groundingPassages>

        <assistantAnswer>{{answer}}</assistantAnswer>

        Reply with a JSON object of the form { "score": number, "reason": string }.

        The score must be an integer from 1 to 5 inclusive:
          5 - every claim is directly supported by the passages.
          4 - well supported, with only trivial unsupported detail.
          3 - mostly supported, but with some claims not found in the passages.
          2 - substantial claims are unsupported by or inconsistent with the passages.
          1 - largely fabricated or contradicted by the passages.

        The reason must be at most 20 words describing what is or is not supported.
        """;
}
