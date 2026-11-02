// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Represents an admitted passage whose whole section and neighbors are read.
/// </summary>
/// <param name="Page">The page URL.</param>
/// <param name="Root">The heading path of the section, down to its H2.</param>
/// <param name="Exact">Whether only chunks with exactly the root heading path belong to the section (the page intro).</param>
/// <param name="Index">The chunk index of the anchor within its page.</param>
/// <param name="Neighbors">The ids of the chunks just before and after the anchor.</param>
record SectionAnchor(string Page, string Root, bool Exact, int Index, string[] Neighbors)
{
    /// <summary>
    /// Creates the anchor of an admitted passage.
    /// </summary>
    /// <param name="passage">The admitted passage, with a chunk id.</param>
    /// <returns>The anchor.</returns>
    public static SectionAnchor Of(Passage passage)
    {
        var prefix = $"{passage.Title} > ";
        var (root, exact) = passage.HeadingPath.StartsWith(prefix, StringComparison.Ordinal)
            ? (prefix + passage.HeadingPath[prefix.Length..].Split(" > ")[0], false)
            : (passage.HeadingPath, true);
        var index = ChunkOrder.Of(passage.ChunkId).Index;
        string[] neighbors = index is >= 0 and < int.MaxValue
            ? [$"{passage.Page.Value}#{index - 1}", $"{passage.Page.Value}#{index + 1}"]
            : [];
        return new(passage.Page.Value, root, exact, index, neighbors);
    }

    /// <summary>Gets the rank of the first anchor of the row's page that the row belongs to, falling back to the first anchor of the page.</summary>
    /// <param name="anchors">The anchors, best first.</param>
    /// <param name="row">The expansion row.</param>
    /// <returns>The rank.</returns>
    public static int Rank(SectionAnchor[] anchors, Passage row)
    {
        var rank = Array.FindIndex(anchors, anchor => anchor.Page == row.Page.Value && anchor.Contains(row));
        return rank >= 0 ? rank : Math.Max(0, Array.FindIndex(anchors, anchor => anchor.Page == row.Page.Value));
    }

    /// <summary>Gets how far the row sits from its anchor within the page.</summary>
    /// <param name="anchors">The anchors, best first.</param>
    /// <param name="row">The expansion row.</param>
    /// <returns>The distance in chunk indexes.</returns>
    public static int Distance(SectionAnchor[] anchors, Passage row) =>
        Math.Abs(ChunkOrder.Of(row.ChunkId).Index - anchors[Rank(anchors, row)].Index);

    bool Contains(Passage row) =>
        row.HeadingPath == Root || (!Exact && row.HeadingPath.StartsWith(Root + " > ", StringComparison.Ordinal)) ||
        (row.ChunkId is not null && Array.IndexOf(Neighbors, row.ChunkId) >= 0);
}
