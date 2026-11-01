// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_row_exceeds_max_length : Specification
{
    const string Header = "| A | B |\n| --- | --- |";

    Chunk[] _chunks = null!;
    Chunk[] _justFits = null!;
    string _cell = null!;

    void Establish() => _cell = new string('x', 3998 - 8);

    void Because()
    {
        var row = $"| a | {_cell} |";
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/row.md"), $"# Row\n\n{Header}\n{row}\n")];
        var fits = $"| a | {new string('y', 4000 - Header.Length - 1 - 8)} |";
        _justFits = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/fits.md"), $"# Fits\n\n{Header}\n{fits}\n")];
    }

    [Fact] void should_split_the_row() => _chunks.Length.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_fragment_within_the_limit_including_the_header() => _chunks.All(chunk => chunk.Content.Length <= 4000).ShouldBeTrue();
    [Fact] void should_repeat_the_header_on_every_fragment() => _chunks.All(chunk => chunk.Content.StartsWith($"{Header}\n", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_lose_cell_characters() => string.Concat(_chunks.Select(chunk => chunk.Content[(Header.Length + 1)..])).Count(character => character == 'x').ShouldEqual(_cell.Length);
    [Fact] void should_keep_a_row_that_exactly_fits_in_one_chunk() => _justFits.Length.ShouldEqual(1);
    [Fact] void should_keep_the_exact_fit_at_the_limit() => _justFits[0].Content.Length.ShouldEqual(4000);
}
