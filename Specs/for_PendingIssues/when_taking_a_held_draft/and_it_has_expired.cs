// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_it_has_expired : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.UnixEpoch);
    PendingIssue? _taken;

    async Task Because()
    {
        var pending = new PendingIssues(_time);
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(new("Cratis", "Chronicle", "Title", "Body", []), scope, _ => Task.FromResult(4UL));
        _time.Advance(PendingIssues.Lifetime);
        _taken = pending.Take(token, scope, 4);
    }

    [Fact] void should_expire_at_the_boundary() => (_taken is null).ShouldBeTrue();
}
