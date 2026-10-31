// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ClarificationButton.when_parsing;

public class and_the_custom_id_is_round_tripped : Specification
{
    [Fact] void should_build_the_id() => ClarificationButton.CustomId(InteractionProduct.Chronicle).ShouldEqual("clarify:chronicle");
    [Fact] void should_read_the_product() => ClarificationButton.Parse("clarify:cli").ShouldEqual(InteractionProduct.Cli);
    [Fact] void should_reject_an_unknown_product() => ClarificationButton.Parse("clarify:nothing").ShouldBeNull();
    [Fact] void should_reject_a_foreign_prefix() => ClarificationButton.Parse("suggest:1").ShouldBeNull();
}
