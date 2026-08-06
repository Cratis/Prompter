// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_deciding_to_answer;

public class and_the_repository_opted_in : Specification
{
    readonly GitHubOptions _options = new() { AnsweringRepositories = ["Cratis/Chronicle"] };
    bool _shouldAnswer;

    void Because() => _shouldAnswer = IssueEvents.ShouldAnswer(
        new("Cratis/Chronicle", 1, "A question", "Body", "url", false, []),
        _options);

    [Fact] void should_answer() => _shouldAnswer.ShouldBeTrue();
}
