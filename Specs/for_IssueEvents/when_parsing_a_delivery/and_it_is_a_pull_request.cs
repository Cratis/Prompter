// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_parsing_a_delivery;

public class and_it_is_a_pull_request : Specification
{
    OpenedIssue? _issue;

    void Because() => _issue = IssueEvents.ParseOpened(
        """
        {
          "action": "opened",
          "repository": { "full_name": "Cratis/Chronicle" },
          "issue": { "number": 7, "title": "A pull request", "pull_request": { "url": "..." } }
        }
        """);

    [Fact] void should_not_answer_a_code_review() => (_issue is null).ShouldBeTrue();
}
