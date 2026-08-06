// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_parsing_a_delivery;

public class and_the_action_is_not_opened : Specification
{
    OpenedIssue? _issue;

    void Because() => _issue = IssueEvents.ParseOpened(
        """{"action":"edited","repository":{"full_name":"Cratis/Chronicle"},"issue":{"number":1}}""");

    [Fact] void should_ignore_it() => (_issue is null).ShouldBeTrue();
}
