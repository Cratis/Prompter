// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents a grounded answer to a question.
/// </summary>
/// <param name="Text">The answer text.</param>
/// <param name="Citations">The documentation pages the answer is grounded in.</param>
/// <param name="Confidence">The top passage score backing the answer.</param>
/// <param name="IsRefusal">Whether Prompter declined to answer for lack of grounding.</param>
/// <param name="Passages">The passages retrieved for the question, best first - what the answer was grounded in.</param>
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

    /// <summary>
    /// Creates a refusal answer for when the documentation does not cover the question well enough.
    /// </summary>
    /// <param name="confidence">The top passage score that fell below the refusal threshold.</param>
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
