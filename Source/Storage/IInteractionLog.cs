// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Defines the log of question/answer interactions. This is the seam behind which the storage of
/// interactions can be swapped - see the Chronicle dogfooding decision record (D-6).
/// </summary>
public interface IInteractionLog
{
    /// <summary>
    /// Records an interaction.
    /// </summary>
    /// <param name="interaction">The interaction to record.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The identifier of the recorded interaction, used to locate the row when feedback arrives.</returns>
    Task<long> Record(Interaction interaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches the Discord message id the answer was delivered on to a recorded interaction, for auditing.
    /// </summary>
    /// <param name="interactionId">The identifier returned by <see cref="Record"/>.</param>
    /// <param name="answerMessageId">The Discord message id the answer (and its feedback buttons) landed on.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Awaitable task.</returns>
    Task SetAnswerMessage(long interactionId, string answerMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the feedback verdict a user gave on a recorded interaction's answer.
    /// </summary>
    /// <param name="interactionId">The identifier returned by <see cref="Record"/>, decoded from the button custom id.</param>
    /// <param name="verdict">The verdict the user gave.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Awaitable task.</returns>
    Task RecordFeedback(long interactionId, FeedbackVerdict verdict, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges interactions older than the configured retention window.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The number of interactions purged.</returns>
    Task<int> PurgeExpired(CancellationToken cancellationToken = default);
}
