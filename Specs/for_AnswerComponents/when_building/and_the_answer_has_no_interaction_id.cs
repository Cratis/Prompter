// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_the_answer_has_no_interaction_id : Specification
{
    IReadOnlyList<ActionRowProperties> _rows = [];

    void Because() => _rows = AnswerComponents.For(new Answer("text", [], 0.8, false, []), AnswerActions.None);

    [Fact] void should_have_no_rows() => _rows.ShouldBeEmpty();
}
