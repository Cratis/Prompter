// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

/// <summary>
/// Fixture is the real cratis.io Cratis Stack page mirror (https://cratis.io/cratis-stack.md, captured 2026-11-01),
/// whose frontmatter description and capability map list every product.
/// </summary>
public class and_page_is_the_real_stack_page : Specification
{
    static readonly string[] _rowProducts = ["Python client", "Go client"];
    static readonly string[] _products = ["Chronicle", "Arc", "Components", "Specifications"];

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/cratis-stack.md"), Fixture.Load("cratis-stack.md"))];

    [Fact] void should_summarize_the_page_from_its_description() => _chunks.Single(chunk => chunk.Id.Value.EndsWith("#summary", StringComparison.Ordinal)).Content.ShouldContain("Every Cratis product in one table");
    [Fact]
    void should_list_the_products_in_the_capability_map() => _chunks.Any(chunk => chunk.HeadingPath.EndsWith("Capability map", StringComparison.Ordinal) &&
        _products.All(product => chunk.Content.Contains(product, StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_split_the_capability_map_into_row_groups() => _chunks.Count(chunk => chunk.Content.StartsWith("| Product | What it does |", StringComparison.Ordinal)).ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_product_row() => Array.TrueForAll(_rowProducts, product => _chunks.Count(chunk => chunk.Content.StartsWith("| Product |", StringComparison.Ordinal) && chunk.Content.Contains(product, StringComparison.Ordinal)) == 1).ShouldBeTrue();
}
