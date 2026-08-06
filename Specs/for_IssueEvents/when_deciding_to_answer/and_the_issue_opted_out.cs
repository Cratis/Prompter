// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_deciding_to_answer;

public class and_the_issue_opted_out : Specification
{
    readonly GitHubOptions _options = new() { AnsweringRepositories = ["Cratis/Chronicle"] };
    bool _shouldAnswer;

    void Because() => _shouldAnswer = IssueEvents.ShouldAnswer(
        new("Cratis/Chronicle", 1, "A question", "Body", "url", false, ["NO-PROMPTER"]),
        _options);

    [Fact] void should_honor_the_label_whatever_its_casing() => _shouldAnswer.ShouldBeFalse();
}
