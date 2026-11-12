// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_parsing_a_delivery;

public class and_optional_fields_are_missing : Specification
{
    OpenedIssue _issue = null!;

    void Because() => _issue = IssueEvents.ParseOpened("""{"action":"opened","repository":{},"issue":{}}""")!;

    [Fact] void should_default_the_repository() => _issue.Repository.ShouldEqual(string.Empty);
    [Fact] void should_default_the_number() => _issue.Number.ShouldEqual(0);
    [Fact] void should_default_the_title() => _issue.Title.ShouldEqual(string.Empty);
    [Fact] void should_default_the_body() => _issue.Body.ShouldEqual(string.Empty);
    [Fact] void should_default_the_url() => _issue.Url.ShouldEqual(string.Empty);
    [Fact] void should_default_the_labels() => _issue.Labels.Count.ShouldEqual(0);
    [Fact] void should_not_infer_a_bot() => _issue.OpenedByBot.ShouldBeFalse();
}
