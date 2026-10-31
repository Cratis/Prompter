// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackButtonRow.when_building;

public class and_actions_are_offered : Specification
{
    [Fact] void should_have_two_buttons_without_actions() => FeedbackButtonRow.For(5).Components.Count().ShouldEqual(2);
    [Fact] void should_add_human_and_documented() => FeedbackButtonRow.For(5, new(new(2, 3), true, true, null)).Components.Count().ShouldEqual(4);
    [Fact] void should_add_request() => FeedbackButtonRow.For(5, new(new(2, 3), false, false, "chronicle")).Components.Count().ShouldEqual(3);
    [Fact] void should_ignore_actions_without_a_question() => FeedbackButtonRow.For(5, new(null, true, true, "x")).Components.Count().ShouldEqual(2);
}
