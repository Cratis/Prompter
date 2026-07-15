// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackButton.when_parsing;

public class and_the_custom_id_encodes_a_down_vote : Specification
{
    FeedbackClick _result = null!;

    void Because() => _result = FeedbackButton.Parse(FeedbackButton.CustomId(FeedbackVerdict.Down, 987))!;

    [Fact] void should_decode_a_click() => _result.ShouldNotBeNull();
    [Fact] void should_round_trip_the_verdict() => _result.Verdict.ShouldEqual(FeedbackVerdict.Down);
    [Fact] void should_round_trip_the_interaction_id() => _result.InteractionId.ShouldEqual(987L);
}
