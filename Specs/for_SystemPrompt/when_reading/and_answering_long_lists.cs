// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_answering_long_lists : Specification
{
    [Fact] void should_keep_one_concise_line_per_item_with_its_citation() => SystemPrompt.Text.ShouldContain("use one concise line per item with its citation");
    [Fact] void should_not_require_enumerating_every_item_of_a_long_list() => SystemPrompt.Text.ShouldContain("do not enumerate every item");
    [Fact] void should_keep_the_full_product_list_complete() => SystemPrompt.Text.ShouldContain("Apart from the full Cratis product list above, which is always complete");
    [Fact] void should_name_the_main_items_with_citations() => SystemPrompt.Text.ShouldContain("name the main");
    [Fact] void should_always_include_the_core_products() => SystemPrompt.Text.ShouldContain("always including the core products the material names (Chronicle, Arc, Components and Fundamentals when");
    [Fact] void should_point_to_the_cited_page_for_the_full_list() => SystemPrompt.Text.ShouldContain("point to the cited page for the full list");
    [Fact] void should_keep_the_prose_cap() => SystemPrompt.Text.ShouldContain("at most 120 words of prose");
}
