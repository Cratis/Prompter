// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_classifying_out_of_scope_questions : Specification
{
    [Fact] public void should_classify_by_what_the_question_asks() => SystemPrompt.Text.ShouldContain("even when an excerpt shares a word with it");
    [Fact] public void should_decline_setting_up_non_cratis_tools() => SystemPrompt.Text.ShouldContain("set up or operate a non-Cratis tool");
    [Fact] public void should_keep_comparisons_with_cratis_in_scope() => SystemPrompt.Text.ShouldContain("comparing another tool with Cratis is in scope");
}
