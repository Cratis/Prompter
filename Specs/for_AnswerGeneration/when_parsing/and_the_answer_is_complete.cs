// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_answer_is_complete : Specification
{
    const string Text = "Cratis offers these products:\n\n- Chronicle stores events [1].\n- Arc handles commands [2].\n\n```csharp\nvar x = 1;";

    GeneratedAnswer _result = null!;

    void Because() => _result = AnswerGeneration.Parse("{\"text\":\"Cratis offers these products:\\n\\n- Chronicle stores events [1].\\n- Arc handles commands [2].\\n\\n```csharp\\nvar x = 1;\",\"category\":\"answered\",\"product\":\"Chronicle\",\"suggestions\":[]}")!;

    [Fact] void should_keep_the_whole_text_unchanged() => _result.Text.ShouldEqual(Text);
    [Fact] void should_keep_the_category() => _result.Category.ShouldEqual(InteractionCategory.Answered);
}
