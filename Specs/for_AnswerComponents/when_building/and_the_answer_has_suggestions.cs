// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_the_answer_has_suggestions : Specification
{
    IReadOnlyList<ActionRowProperties> _rows = [];

    void Because() => _rows = AnswerComponents.For(new Answer("text", [], 0.8, false, []) { InteractionId = 5, Suggestions = ["First?", "Second?"] }, AnswerActions.None);

    [Fact] void should_add_a_row() => _rows.Count.ShouldEqual(2);
    [Fact] void should_label_with_the_suggestions() => _rows[1].Components.OfType<ButtonProperties>().Select(button => button.Label).ShouldEqual(["First?", "Second?"]);
    [Fact] void should_not_put_suggestion_text_in_custom_ids() => _rows.SelectMany(row => row.Components.OfType<ButtonProperties>()).Any(button => button.CustomId.Contains("First") || button.CustomId.Contains("Second")).ShouldBeFalse();
}
