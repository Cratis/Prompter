// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_line_has_many_inline_comments : Specification
{
    Chunk[] _chunks = null!;
    long _elapsedMilliseconds;

    void Because()
    {
        var line = string.Concat(Enumerable.Repeat("a{/* c */}b", 20000));
        var started = TimeProvider.System.GetTimestamp();
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/comments.md"), $"# Comments\n\n{line}\n\nTail {{/* open\n")];
        _elapsedMilliseconds = (long)TimeProvider.System.GetElapsedTime(started).TotalMilliseconds;
    }

    [Fact] void should_remove_every_complete_comment() => string.Concat(_chunks.Select(chunk => chunk.Content)).Contains("{/*", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_text_around_the_comments() => string.Concat(_chunks.Select(chunk => chunk.Content)).ShouldEqual(string.Concat(Enumerable.Repeat("ab", 20000)) + "Tail");
    [Fact] void should_finish_quickly() => _elapsedMilliseconds.ShouldBeLessThan(5000L);
}
