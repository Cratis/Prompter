// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackButton.when_building_a_custom_id;

public class and_the_verdict_is_up : Specification
{
    string _customId = null!;

    void Because() => _customId = FeedbackButton.CustomId(FeedbackVerdict.Up, 12345);

    [Fact] void should_encode_prefix_verdict_and_id() => _customId.ShouldEqual("feedback:up:12345");
}
