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
    /// <returns>Awaitable task.</returns>
    Task Record(Interaction interaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges interactions older than the configured retention window.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The number of interactions purged.</returns>
    Task<int> PurgeExpired(CancellationToken cancellationToken = default);
}
