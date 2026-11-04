// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Represents a retrieved passage with independent ranking and grounding signals.
/// </summary>
/// <param name="Page">The URL of the page the passage comes from.</param>
/// <param name="Title">The title of the page.</param>
/// <param name="HeadingPath">The heading path within the page.</param>
/// <param name="Content">The markdown content of the passage.</param>
/// <param name="Score">The reciprocal-rank-fusion score across lexical and semantic retrieval, used only for ordering.</param>
/// <param name="SemanticSimilarity">The cosine similarity to the question, in [-1, 1], not a correctness probability.</param>
public record Passage(PageUrl Page, string Title, string HeadingPath, string Content, double Score, double SemanticSimilarity)
{
    /// <summary>Gets whether this is the page's frontmatter summary chunk.</summary>
    public bool IsSummary { get; init; }

    /// <summary>Gets whether this passage was looked up to expand an admitted summary.</summary>
    public bool IsPageExpansion { get; init; }

    /// <summary>
    /// Gets whether this passage is a curated overview summary added because the question matched the broad-overview
    /// intent. Its relevance comes from that match, so it is admitted without the semantic-similarity floor; it must
    /// still be citable and its similarity valid, and it still counts toward the passage cap.
    /// </summary>
    public bool IsCurated { get; init; }

    /// <summary>Gets the id of the chunk the passage was read from, when known.</summary>
    public string? ChunkId { get; init; }

    /// <summary>Gets whether this passage was looked up as the section or a neighbor of an admitted passage.</summary>
    public bool IsSectionExpansion { get; init; }

    /// <summary>Gets when the page was last published by a successful index run; <see langword="null"/> when unknown.</summary>
    public DateTimeOffset? IndexedAt { get; init; }

    /// <summary>Gets the page's explicit source last-modified date, when it stated one.</summary>
    public DateTimeOffset? PageLastModifiedAt { get; init; }

    /// <summary>Gets the explicit product version the page stated, without a leading <c language="text">v</c>, when it stated one.</summary>
    public string? ProductVersion { get; init; }

    /// <summary>Checks whether the passage clears the grounding floor: a valid similarity at or above it, or a curated overview summary with a valid similarity.</summary>
    /// <param name="floor">The minimum semantic similarity for ordinary passages.</param>
    /// <returns><see langword="true"/> when the passage may ground an answer.</returns>
    public bool ClearsFloor(double floor) =>
        double.IsFinite(SemanticSimilarity) && SemanticSimilarity is >= -1 and <= 1 && (IsCurated || SemanticSimilarity >= floor);
}
