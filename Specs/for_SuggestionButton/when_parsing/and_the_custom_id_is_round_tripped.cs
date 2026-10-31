// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_SuggestionButton.when_parsing;

public class and_the_custom_id_is_round_tripped : Specification
{
    [Fact] void should_build_the_id() => SuggestionButton.CustomId(2).ShouldEqual("suggest:2");
    [Fact] void should_read_the_index() => SuggestionButton.Parse("suggest:1").ShouldEqual(1);
}
