// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_only_the_title_differs : Specification
{
    const string TitledOne =
        """
        # Title One

        ## Section

        Identical body text.
        """;

    const string TitledTwo =
        """
        # Title Two

        ## Section

        Identical body text.
        """;

    Chunk _one = null!;
    Chunk _two = null!;

    void Because()
    {
        var url = new PageUrl("https://cratis.io/guide.md");
        _one = MarkdownChunker.Chunk(url, TitledOne).Single();
        _two = MarkdownChunker.Chunk(url, TitledTwo).Single();
    }

    [Fact] void should_keep_the_body_identical() => _one.Content.ShouldEqual(_two.Content);
    [Fact] void should_hash_the_title_rename_as_a_change() => _one.Hash.ShouldNotEqual(_two.Hash);
}
