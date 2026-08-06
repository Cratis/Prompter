// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueAnswerComment.when_composing;

public class and_the_answer_has_sources : Specification
{
    string _comment = null!;

    void Because() => _comment = IssueAnswerComment.For(
        new("Append events with the event log.", [new("https://cratis.io/chronicle/events")], 0.9, false, []),
        "no-prompter");

    [Fact] void should_lead_with_the_answer() => _comment.StartsWith("Append events", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_sources() => _comment.ShouldContain("https://cratis.io/chronicle/events");
    [Fact] void should_say_a_bot_wrote_it() => _comment.ShouldContain("Prompter");
    [Fact] void should_say_how_to_stop_it() => _comment.ShouldContain("no-prompter");
}
