// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_only_the_heading_differs : Specification
{
    const string UnderAlpha =
        """
        # Guide

        ## Alpha

        Shared body paragraph that stays identical.
        """;

    const string UnderBeta =
        """
        # Guide

        ## Beta

        Shared body paragraph that stays identical.
        """;

    Chunk _alpha = null!;
    Chunk _beta = null!;

    void Because()
    {
        var url = new PageUrl("https://cratis.io/guide.md");
        _alpha = MarkdownChunker.Chunk(url, UnderAlpha).Single();
        _beta = MarkdownChunker.Chunk(url, UnderBeta).Single();
    }

    [Fact] void should_keep_the_body_identical() => _alpha.Content.ShouldEqual(_beta.Content);
    [Fact] void should_hash_the_heading_rename_as_a_change() => _alpha.Hash.ShouldNotEqual(_beta.Hash);
}
