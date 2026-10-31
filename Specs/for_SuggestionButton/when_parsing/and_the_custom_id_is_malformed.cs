// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_SuggestionButton.when_parsing;

public class and_the_custom_id_is_malformed : Specification
{
    [Fact] void should_reject_an_out_of_range_index() => SuggestionButton.Parse("suggest:3").ShouldBeNull();
    [Fact] void should_reject_a_foreign_prefix() => SuggestionButton.Parse("feedback:1").ShouldBeNull();
    [Fact] void should_reject_a_non_number() => SuggestionButton.Parse("suggest:x").ShouldBeNull();
    [Fact] void should_reject_extra_segments() => SuggestionButton.Parse("suggest:1:2").ShouldBeNull();
}
