// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueNotification.when_announcing_an_issue;

public class and_prompter_could_not_answer_it : Specification
{
    string _message = null!;

    void Because() => _message = IssueNotification.For(
        new("Cratis/Chronicle", 42, "A question", "Body", "url", false, []),
        answered: false);

    [Fact] void should_flag_it_as_a_possible_gap() => _message.ShouldContain("may be a real gap");
}
