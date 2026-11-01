// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_the_description_is_not_a_plain_scalar : Specification
{
    static readonly PageUrl _page = new("https://cratis.io/page.md");

    bool _folded;
    bool _empty;
    bool _tooLong;
    bool _noFrontmatter;

    void Because()
    {
        _folded = HasSummary("---\ndescription: >\n  A folded description.\n---\n\n# Page\n\nBody.");
        _empty = HasSummary("---\ndescription:\n---\n\n# Page\n\nBody.");
        _tooLong = HasSummary($"---\ndescription: {new string('a', 1001)}\n---\n\n# Page\n\nBody.");
        _noFrontmatter = HasSummary("# Page\n\nBody.");
    }

    static bool HasSummary(string markdown) => MarkdownChunker.Chunk(_page, markdown).Any(chunk => chunk.Id.Value.EndsWith("#summary", StringComparison.Ordinal));

    [Fact] void should_skip_a_folded_block_scalar() => _folded.ShouldBeFalse();
    [Fact] void should_skip_an_empty_description() => _empty.ShouldBeFalse();
    [Fact] void should_skip_an_overlong_description() => _tooLong.ShouldBeFalse();
    [Fact] void should_skip_a_page_without_frontmatter() => _noFrontmatter.ShouldBeFalse();
}
