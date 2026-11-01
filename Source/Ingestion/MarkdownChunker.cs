// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Splits a markdown documentation page into retrievable chunks along its heading structure.
/// </summary>
public static partial class MarkdownChunker
{
    const int DefaultMaxChunkLength = 4000;
    const int DefaultMaxTableLength = 1500;
    const int MaximumDescriptionLength = 1000;
    const int MaximumTitleLength = 200;

    [GeneratedRegex(@"^[>|][+-]?[0-9]?$", RegexOptions.None, 1000)]
    private static partial Regex BlockIndicator { get; }

    /// <summary>
    /// Chunks a markdown page into sections split on second and third level headings, keeping the
    /// heading path for context and splitting oversized sections on paragraph boundaries. Tables longer than
    /// <paramref name="maxTableLength"/> are split into row groups that repeat the header, and blocks without blank lines
    /// that exceed <paramref name="maxChunkLength"/> are split at line boundaries. The first piece of a part keeps the
    /// part id; further pieces get a numbered suffix, so unaffected parts keep their id and hash.
    /// </summary>
    /// <param name="page">The URL of the page being chunked.</param>
    /// <param name="markdown">The markdown content of the page.</param>
    /// <param name="maxChunkLength">The maximum length of a single chunk in characters.</param>
    /// <param name="maxTableLength">The length in characters above which a table is split into row groups.</param>
    /// <returns>The chunks for the page.</returns>
    public static IEnumerable<Chunk> Chunk(PageUrl page, string markdown, int maxChunkLength = DefaultMaxChunkLength, int maxTableLength = DefaultMaxTableLength)
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

                var piece = 0;
                foreach (var text in ChunkPieces.Split(trimmed, maxChunkLength, maxTableLength).Select(text => text.Trim()).Where(text => text.Length > 0))
                {
                    var id = piece == 0 ? $"{page.Value}#{index}" : $"{page.Value}#{index}.{piece}";
                    yield return new(
                        new ChunkId(id),
                        page,
                        title,
                        section.HeadingPath,
                        text,
                        ContentHash.For(global::Cratis.Prompter.Ingestion.Chunk.EmbeddingInputFor(title, section.HeadingPath, text)));
                    piece++;
                }

                index++;
            }
        }

        if (Summary(page, markdown, title) is { } summary)
        {
            yield return summary;
        }
    }

    /// <summary>
    /// Builds the short summary chunk for a page from its frontmatter description, when it is a plain single-line scalar.
    /// </summary>
    /// <param name="page">The URL of the page.</param>
    /// <param name="markdown">The markdown content of the page, including frontmatter.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The summary chunk, or <see langword="null"/> when the page has no usable description.</returns>
    static global::Cratis.Prompter.Ingestion.Chunk? Summary(PageUrl page, string markdown, string title)
    {
        var (lines, _) = DocumentationMetadata.Split(markdown);
        var description = DocumentationMetadata.Unique(lines, "description")?.Trim();
        if (string.IsNullOrEmpty(description) || description.Length > MaximumDescriptionLength || BlockIndicator.IsMatch(description))
        {
            return null;
        }

        var frontmatterTitle = DocumentationMetadata.Unique(lines, "title")?.Trim();
        var displayTitle = !string.IsNullOrEmpty(frontmatterTitle) && frontmatterTitle.Length <= MaximumTitleLength && !BlockIndicator.IsMatch(frontmatterTitle)
            ? frontmatterTitle
            : title;
        var headingPath = $"{title} > Summary";
        var content = $"{displayTitle}\n\n{description}";

        return new(
            new ChunkId($"{page.Value}#summary"),
            page,
            title,
            headingPath,
            content,
            ContentHash.For(global::Cratis.Prompter.Ingestion.Chunk.EmbeddingInputFor(title, headingPath, content)));
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
    /// (<c language="csharp">{/* ... */}</c>) and block-level component tags (<c language="csharp">&lt;CardGrid&gt;</c>, <c language="csharp">&lt;TopicHero …&gt;</c>,
    /// self-closing <c language="csharp">&lt;SimpleCard … /&gt;</c>, including multi-line forms) - while keeping the prose
    /// children of paired component tags (e.g. hero text). Everything inside fenced code blocks is left
    /// untouched. Component tags are recognized as elements whose name starts with an uppercase letter, the
    /// MDX convention, so lowercase HTML and inline generics such as <c language="csharp">List&lt;T&gt;</c> are left alone.
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
    /// Removes every complete <c language="csharp">{/* ... */}</c> comment on the line; a comment left open sets
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
