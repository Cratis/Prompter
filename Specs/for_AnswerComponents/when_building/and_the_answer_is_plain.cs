// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_the_answer_is_plain : Specification
{
    IReadOnlyList<ActionRowProperties> _rows = [];

    void Because() => _rows = AnswerComponents.For(new Answer("text", [], 0.8, false, []) { InteractionId = 5 }, AnswerActions.None);

    [Fact] void should_have_exactly_the_feedback_row() => _rows.Count.ShouldEqual(1);
    [Fact] void should_equal_the_feedback_row() => _rows[0].Components.OfType<ButtonProperties>().Select(button => button.CustomId).ShouldEqual(FeedbackButtonRow.For(5).Components.OfType<ButtonProperties>().Select(button => button.CustomId));
}
