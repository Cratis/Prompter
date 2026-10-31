// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_SuggestionButton.when_finding_the_label;

public class and_the_button_exists : Specification
{
    static readonly (string, string)[] _buttons = [("feedback:up:1", "👍 Helpful"), ("suggest:0", "First?"), ("suggest:1", "Second?")];

    [Fact] void should_find_the_label() => SuggestionButton.LabelAt(_buttons, 1).ShouldEqual("Second?");
    [Fact] void should_ignore_foreign_prefixes() => SuggestionButton.LabelAt(_buttons, 2).ShouldBeNull();
    [Fact] void should_not_match_a_feedback_button() => SuggestionButton.LabelAt([("feedback:up:1", "x")], 0).ShouldBeNull();
}
