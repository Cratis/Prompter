// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents a grounded answer to a question.
/// </summary>
/// <param name="Text">The answer text.</param>
/// <param name="Citations">The documentation pages the answer is grounded in.</param>
/// <param name="Confidence">The RRF-first admitted passage's cosine similarity, or highest valid candidate similarity on refusal; not a correctness probability.</param>
/// <param name="IsRefusal">Whether Prompter declined to answer for lack of grounding.</param>
/// <param name="Passages">The admitted passages in RRF order, or all retrieved candidates on refusal.</param>
public record Answer(
    string Text,
    IEnumerable<PageUrl> Citations,
    double Confidence,
    bool IsRefusal,
    IReadOnlyList<Passage> Passages)
{
    /// <summary>
    /// Gets the id of the interaction row this answer was recorded as, when it was recorded (Discord and
    /// CLI answers are; a fresh <see cref="Refusal"/> before recording is not). Feedback buttons carry this
    /// id so a verdict can be written back to the row.
    /// </summary>
    public long? InteractionId { get; init; }

    /// <summary>Gets the index provenance of each cited page, in citation order; empty when there are no citations.</summary>
    public IReadOnlyList<CitationProvenance> CitationProvenance { get; init; } = [];

    /// <summary>Gets the model's triage category hint; <see cref="InteractionCategory.Unknown"/> unless generated in the same call.</summary>
    public InteractionCategory Category { get; init; } = InteractionCategory.Unknown;

    /// <summary>Gets the product the question concerns; <see cref="InteractionProduct.OtherOrUnknown"/> unless generated in the same call.</summary>
    public InteractionProduct Product { get; init; } = InteractionProduct.OtherOrUnknown;

    /// <summary>Gets whether this request fell back to current-only conversation context.</summary>
    public bool ContextDegraded { get; init; }

    /// <summary>Gets whether secret-shaped input was removed before answering.</summary>
    public bool SecretsRedacted { get; init; }

    /// <summary>Gets the follow-up questions the same excerpts also answer, already filtered and bounded; empty when there are none.</summary>
    public IReadOnlyList<string> Suggestions { get; init; } = [];

    /// <summary>Gets the "which product do you mean?" question when the answer is a clarification instead of an answer; otherwise <see langword="null"/>.</summary>
    public Clarification? Clarification { get; init; }

    /// <summary>Gets the shape of answer the question asked for.</summary>
    public AnswerMode Mode { get; init; } = AnswerMode.Standard;

    /// <summary>
    /// Creates a refusal answer for when the documentation does not cover the question well enough.
    /// </summary>
    /// <param name="confidence">The highest valid retrieved cosine similarity (zero if there are no valid candidates).</param>
    /// <param name="passages">The retrieved passages that were not confident enough to answer from.</param>
    /// <returns>The refusal <see cref="Answer"/>.</returns>
    public static Answer Refusal(double confidence, IReadOnlyList<Passage> passages) => new(
        "I couldn't find anything in the Cratis documentation that answers this with confidence. " +
        "You might get a better answer from a human - or the docs might be missing this, which is worth reporting.",
        [],
        confidence,
        IsRefusal: true,
        passages);
}
