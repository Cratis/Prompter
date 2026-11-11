// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reads the title, body and optional summary of a markdown page.</summary>
internal sealed partial class MarkdownPage
{
    /// <summary>The maximum title and heading length in UTF-16 characters.</summary>
    internal const int MaximumTitleLength = 200;
    const int MaximumDescriptionLength = 1000;
    readonly PageUrl _page;
    readonly string _markdown;

    /// <summary>Initializes a new instance of the <see cref="MarkdownPage"/> class.</summary>
    /// <param name="page">The page URL.</param>
    /// <param name="markdown">The raw page markdown.</param>
    internal MarkdownPage(PageUrl page, string markdown)
    {
        _page = page;
        _markdown = markdown;
        Lines = MdxContent.Strip(StripFrontmatter()).Split('\n');
        var titleLine = Lines.FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        Title = MarkdownSections.BoundHeading(titleLine is null ? FallbackTitle().AsSpan() : titleLine.AsSpan(2).Trim(), MaximumTitleLength);
    }

    /// <summary>Gets the cleaned markdown lines.</summary>
    internal string[] Lines { get; }

    /// <summary>Gets the bounded page title.</summary>
    internal string Title { get; }

    [GeneratedRegex(@"^[>|][+-]?[0-9]?$", RegexOptions.None, 1000)]
    private static partial Regex BlockIndicator { get; }

    /// <summary>Builds a summary from a usable single-line frontmatter description.</summary>
    /// <param name="output">The remaining page-wide embedding allowance.</param>
    /// <returns>The summary, if present and affordable.</returns>
    internal Chunk? Summary(EmbeddingOutputBudget output)
    {
        var (lines, _) = DocumentationMetadata.Split(_markdown);
        var description = DocumentationMetadata.Unique(lines, "description")?.Trim();
        if (!UsableDescription(description))
        {
            return null;
        }

        var frontmatterTitle = DocumentationMetadata.Unique(lines, "title")?.Trim();
        var displayTitle = UsableSummaryTitle(frontmatterTitle) ? frontmatterTitle : Title;
        var headingPath = $"{Title} > Summary";
        var content = $"{displayTitle}\n\n{description}";
        if (!output.TryTake(new(Title, headingPath, content)))
        {
            return null;
        }

        return new(
            new ChunkId($"{_page.Value}#summary"),
            _page,
            Title,
            headingPath,
            content,
            ContentHash.For(Chunk.EmbeddingInputFor(Title, headingPath, content)));
    }

    static bool UsableDescription(string? description) =>
        !string.IsNullOrEmpty(description) && description.Length <= MaximumDescriptionLength && !BlockIndicator.IsMatch(description);

    static bool UsableSummaryTitle(string? title) =>
        !string.IsNullOrEmpty(title) && title.Length <= MaximumTitleLength && !BlockIndicator.IsMatch(title);

    string FallbackTitle()
    {
        var frontmatterTitle = DocumentationMetadata.Unique(DocumentationMetadata.Split(_markdown).Lines, "title")?.Trim();
        return string.IsNullOrEmpty(frontmatterTitle) || BlockIndicator.IsMatch(frontmatterTitle) ? PageTitleFrom() : frontmatterTitle;
    }

    string PageTitleFrom()
    {
        var segments = _page.Value.TrimEnd('/').Split('/');
        return segments.Length == 0 ? _page.Value : segments[^1].Replace(".md", string.Empty, StringComparison.Ordinal);
    }

    string StripFrontmatter()
    {
        if (!_markdown.StartsWith("---", StringComparison.Ordinal))
        {
            return _markdown;
        }

        var end = _markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return _markdown;
        }

        var afterMarker = _markdown.IndexOf('\n', end + 1);
        return afterMarker < 0 ? string.Empty : _markdown[(afterMarker + 1)..];
    }
}
