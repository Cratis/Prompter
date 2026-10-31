// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_AnswerActionButton.when_parsing;

public class and_ids_are_validated : Specification
{
    [Fact] void should_round_trip_human() => AnswerActionButton.Parse(AnswerActionButton.CustomId("human", new(2, 3))).ShouldEqual(new("human", new(2, 3), null));
    [Fact] void should_round_trip_documented() => AnswerActionButton.Parse(AnswerActionButton.CustomId("documented", new(2, 3))).ShouldEqual(new("documented", new(2, 3), null));
    [Fact] void should_round_trip_request() => AnswerActionButton.Parse(AnswerActionButton.RequestCustomId(new(2, 3), "chronicle")).ShouldEqual(new("request", new(2, 3), "chronicle"));
    [Fact] void should_fall_back_for_invalid_product() => AnswerActionButton.Parse(AnswerActionButton.RequestCustomId(new(2, 3), "bad:key"))!.Product.ShouldEqual("unknown");
    [Fact] void should_reject_invalid_product() => AnswerActionButton.Parse("request:2:3:BAD").ShouldBeNull();
    [Fact] void should_reject_unknown_action() => AnswerActionButton.Parse("other:2:3").ShouldBeNull();
    [Fact] void should_reject_missing_parts() => AnswerActionButton.Parse("human:2").ShouldBeNull();
    [Fact] void should_reject_signed_ids() => AnswerActionButton.Parse("human:+2:3").ShouldBeNull();
    [Fact] void should_fit_discord_limit() => (AnswerActionButton.RequestCustomId(new(ulong.MaxValue, ulong.MaxValue), new string('a', 40)).Length <= 100).ShouldBeTrue();
}
