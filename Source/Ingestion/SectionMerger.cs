// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Merges tiny sections into a neighbor under the same second level heading so a heading is not indexed alone.
/// </summary>
static class SectionMerger
{
    const int MinSectionLength = 200;

    /// <summary>
    /// Merges tiny sections.
    /// </summary>
    /// <param name="sections">The sections of a page, in order.</param>
    /// <param name="maxChunkLength">The longest a merged section may be.</param>
    /// <returns>The sections after merging.</returns>
    public static List<Section> Merge(List<Section> sections, int maxChunkLength)
    {
        var merged = new List<Section>(sections);
        while (TryMergeOne(merged, maxChunkLength))
        {
        }

        return merged;
    }

    static bool TryMergeOne(List<Section> sections, int maxChunkLength)
    {
        for (var index = 0; index < sections.Count; index++)
        {
            if (Tiny(sections[index]) && TryMergeNeighbors(sections, index, maxChunkLength))
            {
                return true;
            }
        }

        return false;
    }

    static bool TryMergeNeighbors(List<Section> sections, int index, int maxChunkLength)
    {
        foreach (var neighbor in new[] { index - 1, index + 1 })
        {
            if (CanMerge(sections, index, neighbor) && TryMergePair(sections, Math.Min(index, neighbor), maxChunkLength))
            {
                return true;
            }
        }

        return false;
    }

    static bool CanMerge(List<Section> sections, int index, int neighbor) =>
        neighbor >= 0 && neighbor < sections.Count && sections[neighbor].Root == sections[index].Root && !HasFence(sections[neighbor]);

    static bool TryMergePair(List<Section> sections, int first, int maxChunkLength)
    {
        var combined = Combine(sections[first], sections[first + 1]);
        if (combined.Content.Length > maxChunkLength)
        {
            return false;
        }

        sections[first] = combined;
        sections.RemoveAt(first + 1);
        return true;
    }

    static bool Tiny(Section section) => section.Content.Trim() is { Length: > 0 and < MinSectionLength } && !HasFence(section);

    static bool HasFence(Section section) => section.Content.Contains("```", StringComparison.Ordinal) || section.Content.Contains("~~~", StringComparison.Ordinal);

    static Section Combine(Section first, Section second)
    {
        var (path, heading) = CommonPath(first, second);
        return new(path, $"{Inline(first, path)}\n\n{Inline(second, path)}", first.Root, heading);
    }

    static (string Path, string? Heading) CommonPath(Section first, Section second)
    {
        if (first.HeadingPath == second.HeadingPath || second.HeadingPath.StartsWith(first.HeadingPath + " > ", StringComparison.Ordinal))
        {
            return (first.HeadingPath, first.Heading);
        }

        return first.HeadingPath.StartsWith(second.HeadingPath + " > ", StringComparison.Ordinal)
            ? (second.HeadingPath, second.Heading)
            : (first.Root, null);
    }

    static string Inline(Section section, string path) =>
        section.HeadingPath != path && section.Heading is { } heading
            ? $"### {heading}\n{section.Content.Trim()}"
            : section.Content.Trim();
}
