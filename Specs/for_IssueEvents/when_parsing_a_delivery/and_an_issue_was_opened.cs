// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_parsing_a_delivery;

public class and_an_issue_was_opened : Specification
{
    OpenedIssue _issue = null!;

    void Because() => _issue = IssueEvents.ParseOpened("""
        {
          "action": "opened",
          "repository": { "full_name": "Cratis/Chronicle" },
          "issue": {
            "number": 42,
            "title": "Projection stops after a rename",
            "body": "It stops.",
            "html_url": "https://github.com/Cratis/Chronicle/issues/42",
            "user": { "type": "User" },
            "labels": [ { "name": "bug" } ]
          }
        }
        """)!;

    [Fact] void should_read_the_repository() => _issue.Repository.ShouldEqual("Cratis/Chronicle");
    [Fact] void should_read_the_repository_name_without_its_owner() => _issue.RepositoryName.ShouldEqual("Chronicle");
    [Fact] void should_read_the_number() => _issue.Number.ShouldEqual(42);
    [Fact] void should_read_the_title() => _issue.Title.ShouldEqual("Projection stops after a rename");
    [Fact] void should_read_the_labels() => _issue.Labels.ShouldContain("bug");
    [Fact] void should_not_mistake_a_person_for_a_bot() => _issue.OpenedByBot.ShouldBeFalse();
    [Fact] void should_ask_the_title_and_body_together() => _issue.AsQuestion.ShouldEqual("Projection stops after a rename\n\nIt stops.");
}
