// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Defines the retrievable passages of the indexed documentation.
/// </summary>
public interface IPassages
{
    /// <summary>
    /// Searches for the passages most relevant to a question using hybrid retrieval -
    /// lexical (BM25 style full text) and semantic (embedding cosine similarity) fused with reciprocal rank fusion.
    /// </summary>
    /// <param name="question">The question to find passages for.</param>
    /// <param name="limit">The maximum number of passages to return.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The most relevant passages, best first.</returns>
    Task<IEnumerable<Passage>> Search(Question question, int limit, CancellationToken cancellationToken = default);
}
