// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>
/// Defines the answers Prompter can give - grounded in the indexed documentation.
/// </summary>
public interface IAnswers
{
    /// <summary>
    /// Produces a grounded answer for a question, or a refusal when retrieval confidence is too low.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="source">Where the question came from, e.g. "discord-mention".</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The <see cref="Answer"/>.</returns>
    Task<Answer> For(Question question, string source, CancellationToken cancellationToken = default);
}
