// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueNotification.when_announcing_an_issue;

public class and_answering_was_not_attempted : Specification
{
    string _message = null!;

    void Because() => _message = IssueNotification.For(
        new("Cratis/Chronicle", 42, "A question", "Body", "url", false, []),
        answered: null);

    [Fact] void should_still_announce_it() => _message.ShouldContain("#42");
    [Fact] void should_claim_nothing_about_answering() => _message.Contains("docs", StringComparison.Ordinal).ShouldBeFalse();
}
