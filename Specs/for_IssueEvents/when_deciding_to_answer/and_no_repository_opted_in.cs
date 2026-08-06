// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_deciding_to_answer;

public class and_no_repository_opted_in : Specification
{
    bool _shouldAnswer;

    void Because() => _shouldAnswer = IssueEvents.ShouldAnswer(
        new("Cratis/Chronicle", 1, "A question", "Body", "url", false, []),
        new());

    [Fact] void should_answer_nowhere_by_default() => _shouldAnswer.ShouldBeFalse();
}
