// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_verifying_the_content_hash : Specification
{
    const string Markdown =
        """
        # Event Sourcing

        Chronicle stores events as facts.

        ## Appending events

        Use the event log to append.
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/chronicle/events.md"), Markdown)];

    [Fact]
    void should_hash_the_full_embedded_string_of_every_chunk() =>
        _chunks.All(chunk => chunk.Hash == ContentHash.For(chunk.EmbeddingInput)).ShouldBeTrue();
}
