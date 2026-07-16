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
        var content = StripMdxComponents(StripFrontmatter(markdown));
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
                    ContentHash.For(global::Cratis.Prompter.Ingestion.Chunk.EmbeddingInputFor(title, section.HeadingPath, trimmed)));
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

    /// <summary>
    /// Removes the MDX noise that survives in the markdown mirrors - module imports, JSX expression comments
    /// (<c>{/* ... */}</c>) and block-level component tags (<c>&lt;CardGrid&gt;</c>, <c>&lt;TopicHero …&gt;</c>,
    /// self-closing <c>&lt;SimpleCard … /&gt;</c>, including multi-line forms) - while keeping the prose
    /// children of paired component tags (e.g. hero text). Everything inside fenced code blocks is left
    /// untouched. Component tags are recognized as elements whose name starts with an uppercase letter, the
    /// MDX convention, so lowercase HTML and inline generics such as <c>List&lt;T&gt;</c> are left alone.
    /// </summary>
    /// <param name="content">The markdown content to clean.</param>
    /// <returns>The markdown with MDX noise removed.</returns>
    static string StripMdxComponents(string content)
    {
        var lines = content.Split('\n');
        var result = new StringBuilder();
        var inCodeFence = false;
        var inComment = false;
        var inTag = false;

        foreach (var raw in lines)
        {
            var line = raw;

            if (inCodeFence)
            {
                result.Append(line).Append('\n');
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCodeFence = false;
                }

                continue;
            }

            if (inComment)
            {
                var close = line.IndexOf("*/}", StringComparison.Ordinal);
                if (close < 0)
                {
                    continue;
                }

                line = line[(close + 3)..];
                inComment = false;
            }

            if (inTag)
            {
                var close = line.IndexOf('>', StringComparison.Ordinal);
                if (close < 0)
                {
                    continue;
                }

                line = line[(close + 1)..];
                inTag = false;
            }

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                result.Append(line).Append('\n');
                inCodeFence = true;
                continue;
            }

            line = RemoveInlineComments(line, ref inComment);

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("import ", StringComparison.Ordinal) && trimmed.Contains(" from ", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsComponentTagStart(trimmed))
            {
                var close = line.IndexOf('>', StringComparison.Ordinal);
                if (close < 0)
                {
                    inTag = true;
                    continue;
                }

                line = line[(close + 1)..];
            }

            if (line.Trim().Length == 0)
            {
                result.Append('\n');
                continue;
            }

            result.Append(line.TrimEnd()).Append('\n');
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes every complete <c>{/* ... */}</c> comment on the line; a comment left open sets
    /// <paramref name="inComment"/> and truncates the line at its start.
    /// </summary>
    /// <param name="line">The line to clean.</param>
    /// <param name="inComment">Set to <see langword="true"/> when the line opens a comment that stays open.</param>
    /// <returns>The line with complete inline comments removed.</returns>
    static string RemoveInlineComments(string line, ref bool inComment)
    {
        int open;
        while ((open = line.IndexOf("{/*", StringComparison.Ordinal)) >= 0)
        {
            var close = line.IndexOf("*/}", open + 3, StringComparison.Ordinal);
            if (close < 0)
            {
                inComment = true;
                return line[..open];
            }

            line = line[..open] + line[(close + 3)..];
        }

        return line;
    }

    /// <summary>
    /// Determines whether the trimmed line begins a JSX/MDX component tag - an opening, closing or
    /// self-closing tag whose element name starts with an uppercase letter.
    /// </summary>
    /// <param name="trimmed">The line with leading whitespace already removed.</param>
    /// <returns><see langword="true"/> when the line begins a component tag; otherwise <see langword="false"/>.</returns>
    static bool IsComponentTagStart(string trimmed)
    {
        if (trimmed.Length < 2 || trimmed[0] != '<')
        {
            return false;
        }

        var index = trimmed[1] == '/' ? 2 : 1;

        return index < trimmed.Length && char.IsAsciiLetterUpper(trimmed[index]);
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
