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
    const int DefaultMaxTableLength = 1500;

    /// <summary>
    /// Chunks a markdown page into sections split on second and third level headings, keeping the
    /// heading path for context and splitting oversized sections on paragraph boundaries. Tables longer than
    /// <paramref name="maxTableLength"/> are split into row groups that repeat the header, and blocks without blank lines
    /// that exceed <paramref name="maxChunkLength"/> are split at line boundaries. Tiny sections (under 200 characters
    /// without a code fence) are merged with a neighbor under the same second level heading, keeping their heading
    /// inline, so a heading is never indexed alone. The page title is the level one heading, else the top level
    /// frontmatter title, else the slug. The first piece of a part keeps the
    /// part id; further pieces get a numbered suffix, so unaffected parts keep their id and hash.
    /// </summary>
    /// <param name="page">The URL of the page being chunked.</param>
    /// <param name="markdown">The markdown content of the page.</param>
    /// <param name="maxChunkLength">The maximum length of a single chunk in characters.</param>
    /// <param name="maxTableLength">The length in characters above which a table is split into row groups.</param>
    /// <returns>The chunks for the page.</returns>
    public static IEnumerable<Chunk> Chunk(PageUrl page, string markdown, int maxChunkLength = DefaultMaxChunkLength, int maxTableLength = DefaultMaxTableLength)
    {
        var source = new MarkdownPage(page, markdown);
        var title = source.Title;
        var output = new EmbeddingOutputBudget(Math.Max(8192L, markdown.Length * 64L));

        var sections = SectionMerger.Merge(MarkdownSections.Split(source.Lines, title, MarkdownPage.MaximumTitleLength), maxChunkLength);
        var index = 0;

        foreach (var section in sections)
        {
            var fence = new FenceState();
            foreach (var trimmed in SplitOnParagraphs(section.Content, maxChunkLength).Select(part => part.Trim()).Where(part => part.Length > 0))
            {
                var piece = 0;
                var texts = ChunkPieces.Split(trimmed, maxChunkLength, maxTableLength, fence)
                    .Select(text => text.Trim()).Where(text => text.Length > 0)
                    .TakeWhile(text => output.TryTake(new(title, section.HeadingPath, text)));
                foreach (var text in texts)
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

                if (output.Exhausted)
                {
                    yield break;
                }

                index++;
            }
        }

        if (source.Summary(output) is { } summary)
        {
            yield return summary;
        }
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
