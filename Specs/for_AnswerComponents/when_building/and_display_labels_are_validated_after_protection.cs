// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_AnswerComponents.when_building;

public class and_display_labels_are_validated_after_protection : Specification
{
    [Theory]
    [InlineData("```\n\n```")]
    [InlineData("```\n```")]
    [InlineData("```\nQuestion?\nWithout a closing fence")]
    [InlineData("Question?\u0001")]
    public void should_omit_invalid_labels_without_creating_empty_rows(string suggestion)
    {
        var answer = Answer.Refusal(0.9, []) with { Text = "Grounded [1].", IsRefusal = false, Suggestions = [suggestion] };
        AnswerComponents.For(answer, AnswerActions.None).ShouldBeEmpty();
        answer.Suggestions.Single().ShouldEqual(suggestion);
    }

    [Fact]
    void should_not_truncate_oversized_markup_or_shift_other_question_indices()
    {
        var unsafeMarkup = $"[click](https://evil.invalid/{new string('x', 81)})?";
        var answer = Answer.Refusal(0.9, []) with { Text = "Grounded [1].", IsRefusal = false, Suggestions = [unsafeMarkup, "How are events modeled?"] };
        var buttons = AnswerComponents.For(answer, AnswerActions.None).Single().Components.Cast<ButtonProperties>().ToArray();
        buttons.Length.ShouldEqual(1);
        buttons[0].Label.ShouldEqual("How are events modeled?");
        buttons[0].CustomId.ShouldEqual(SuggestionButton.CustomId(1));
        buttons[0].Style.ShouldEqual(ButtonStyle.Secondary);
        answer.Suggestions[0].ShouldEqual(unsafeMarkup);
    }
}
