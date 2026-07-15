// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Splits a markdown documentation page into retrievable chunks along its heading structure.
/// </summary>
public static class MarkdownChunker
{
    const int DefaultMaxChunkLength = 4000;

    /// <summary>
    /// Chunks a markdown page into sections split on second and third level headings, keeping the
    /// heading path for context and splitting oversized sections on paragraph boundaries.
    /// </summary>
    /// <param name="page">The URL of the page being chunked.</param>
    /// <param name="markdown">The markdown content of the page.</param>
    /// <param name="maxChunkLength">The maximum length of a single chunk in characters.</param>
    /// <returns>The chunks for the page.</returns>
    public static IEnumerable<Chunk> Chunk(PageUrl page, string markdown, int maxChunkLength = DefaultMaxChunkLength)
    {
        var content = StripFrontmatter(markdown);
        var lines = content.Split('\n');
        var title = lines.FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
            ?? PageTitleFrom(page);

        var sections = SplitIntoSections(lines, title);
        var index = 0;

        foreach (var section in sections)
        {
            foreach (var part in SplitOnParagraphs(section.Content, maxChunkLength))
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                yield return new(
                    new ChunkId($"{page.Value}#{index}"),
                    page,
                    title,
                    section.HeadingPath,
                    trimmed,
                    ContentHash.For(trimmed));
                index++;
            }
        }
    }

    static string PageTitleFrom(PageUrl page)
    {
        var segments = page.Value.TrimEnd('/').Split('/');

        return segments.Length == 0 ? page.Value : segments[^1].Replace(".md", string.Empty, StringComparison.Ordinal);
    }

    static string StripFrontmatter(string markdown)
    {
        if (!markdown.StartsWith("---", StringComparison.Ordinal))
        {
            return markdown;
        }

        var end = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return markdown;
        }

        var afterMarker = markdown.IndexOf('\n', end + 1);

        return afterMarker < 0 ? string.Empty : markdown[(afterMarker + 1)..];
    }

    static List<(string HeadingPath, string Content)> SplitIntoSections(string[] lines, string title)
    {
        var sections = new List<(string HeadingPath, string Content)>();
        var currentSecondLevel = string.Empty;
        var currentPath = title;
        var current = new StringBuilder();
        var inCodeFence = false;

        void CloseSection()
        {
            if (current.Length > 0)
            {
                sections.Add((currentPath, current.ToString()));
                current.Clear();
            }
        }

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
            }

            if (!inCodeFence && line.StartsWith("## ", StringComparison.Ordinal))
            {
                CloseSection();
                currentSecondLevel = line[3..].Trim();
                currentPath = $"{title} > {currentSecondLevel}";
                continue;
            }

            if (!inCodeFence && line.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseSection();
                var thirdLevel = line[4..].Trim();
                currentPath = currentSecondLevel.Length == 0
                    ? $"{title} > {thirdLevel}"
                    : $"{title} > {currentSecondLevel} > {thirdLevel}";
                continue;
            }

            if (!inCodeFence && line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            if (!inCodeFence && line.StartsWith("import ", StringComparison.Ordinal) && line.Contains(" from ", StringComparison.Ordinal))
            {
                continue;
            }

            current.AppendLine(line.TrimEnd());
        }

        CloseSection();

        return sections;
    }

    static IEnumerable<string> SplitOnParagraphs(string content, int maxChunkLength)
    {
        if (content.Length <= maxChunkLength)
        {
            yield return content;
            yield break;
        }

        var paragraphs = content.Split("\n\n");
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (current.Length > 0 && current.Length + paragraph.Length + 2 > maxChunkLength)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(paragraph);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
