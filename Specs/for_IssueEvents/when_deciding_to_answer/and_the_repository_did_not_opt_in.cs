// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_deciding_to_answer;

public class and_the_repository_did_not_opt_in : Specification
{
    bool _shouldAnswer;

    void Because() => _shouldAnswer = IssueEvents.ShouldAnswer(
        new("Cratis/Arc", 1, "A question", "Body", "url", false, []),
        new() { AnsweringRepositories = ["Cratis/Chronicle"] });

    [Fact] void should_stay_out_of_it() => _shouldAnswer.ShouldBeFalse();
}
