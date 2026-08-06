// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_deciding_to_answer;

public class and_a_bot_opened_it : Specification
{
    bool _shouldAnswer;

    void Because() => _shouldAnswer = IssueEvents.ShouldAnswer(
        new("Cratis/Chronicle", 1, "A question", "Body", "url", true, []),
        new() { AnsweringRepositories = ["Cratis/Chronicle"] });

    [Fact] void should_not_talk_to_another_bot() => _shouldAnswer.ShouldBeFalse();
}
