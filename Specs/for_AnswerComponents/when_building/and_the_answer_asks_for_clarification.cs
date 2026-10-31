// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_the_answer_asks_for_clarification : Specification
{
    IReadOnlyList<ActionRowProperties> _rows = [];

    void Because() => _rows = AnswerComponents.For(new Answer("text", [], 0.8, false, []) { Clarification = new([InteractionProduct.Chronicle, InteractionProduct.Arc]), Suggestions = ["First?"] }, AnswerActions.None);

    [Fact] void should_have_only_the_clarification_row() => _rows.Count.ShouldEqual(1);
    [Fact] void should_have_a_button_per_product() => _rows[0].Components.OfType<ButtonProperties>().Select(button => button.CustomId).ShouldEqual(["clarify:chronicle", "clarify:arc"]);
}
