// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueNotification.when_announcing_an_issue;

public class and_prompter_answered_it : Specification
{
    string _message = null!;

    void Because() => _message = IssueNotification.For(
        new("Cratis/Chronicle", 42, "A question", "Body", "https://github.com/Cratis/Chronicle/issues/42", false, []),
        answered: true);

    [Fact] void should_name_the_repository() => _message.ShouldContain("Cratis/Chronicle");
    [Fact] void should_link_the_issue() => _message.ShouldContain("https://github.com/Cratis/Chronicle/issues/42");
    [Fact] void should_say_it_was_answered() => _message.ShouldContain("answered it from the docs");
}
