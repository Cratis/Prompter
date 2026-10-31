// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options controlling retrieval and answer generation behavior.
/// </summary>
public class AnsweringOptions
{
    /// <summary>
    /// Gets or sets the maximum number of passages given to the model as grounding context.
    /// </summary>
    public int MaxPassages { get; set; } = 8;

    /// <summary>
    /// Gets or sets the legacy RRF threshold. Ignored; migrate overrides to <see cref="MinSemanticSimilarity"/>.
    /// </summary>
    public double MinScore { get; set; } = 0.02;

    /// <summary>
    /// Gets or sets the minimum cosine similarity for every passage admitted to answer context.
    /// The default is an uncalibrated initial policy, not a correctness probability or recall guarantee.
    /// </summary>
    public double MinSemanticSimilarity { get; set; } = 0.5;

    /// <summary>
    /// Gets whether the semantic cutoff is finite and in (0, 1].
    /// </summary>
    public bool SimilarityIsValid => double.IsFinite(MinSemanticSimilarity) && MinSemanticSimilarity is > 0 and <= 1;

    /// <summary>
    /// Gets or sets the maximum number of output tokens for a generated answer.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 768;
}
