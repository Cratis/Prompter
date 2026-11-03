// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_enumerating_every_product : Specification
{
    [Fact] void should_require_naming_every_product_from_the_capability_map() => SystemPrompt.Text.ShouldContain("name every product from the capability map");
    [Fact] void should_forbid_shortening_the_list_to_a_few_highlights() => SystemPrompt.Text.ShouldContain("Never shorten this to a few highlights or drop a product");
    [Fact] void should_forbid_naming_the_capability_map_or_excerpts_while_listing() => SystemPrompt.Text.ShouldContain("the list by naming the capability map, the excerpts or commenting on whether it is complete");
    [Fact] void should_hold_the_no_excerpts_rule_even_when_exhaustive() => SystemPrompt.Text.ShouldContain("mentioning excerpts holds here too, even under pressure to be exhaustive");
}
