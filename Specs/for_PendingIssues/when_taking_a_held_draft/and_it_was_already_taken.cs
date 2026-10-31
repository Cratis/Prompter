// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_it_was_already_taken : Specification
{
    PendingIssue? _second;

    async Task Because()
    {
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(new("Cratis", "Chronicle", "Title", "Body", []), scope, _ => Task.FromResult(4UL));
        pending.Take(token, scope, 4);
        _second = pending.Take(token, scope, 4);
    }

    [Fact] void should_not_let_a_double_click_file_twice() => (_second is null).ShouldBeTrue();
}
