// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueEvents.when_parsing_a_delivery;

public class and_a_bot_opened_the_issue : Specification
{
    OpenedIssue _issue = null!;

    void Because() => _issue = IssueEvents.ParseOpened(
        """
        {
          "action": "opened",
          "repository": { "full_name": "Cratis/Chronicle" },
          "issue": { "number": 8, "title": "Automated", "user": { "type": "Bot" } }
        }
        """)!;

    [Fact] void should_recognize_the_bot() => _issue.OpenedByBot.ShouldBeTrue();
}
