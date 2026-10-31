// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_the_answer_is_a_refusal_with_suggestions : Specification
{
    IReadOnlyList<ActionRowProperties> _rows = [];

    void Because() => _rows = AnswerComponents.For(Answer.Refusal(0, []) with { InteractionId = 5, Suggestions = ["First?"] }, AnswerActions.None);

    [Fact] void should_only_have_the_feedback_row() => _rows.Count.ShouldEqual(1);
}
