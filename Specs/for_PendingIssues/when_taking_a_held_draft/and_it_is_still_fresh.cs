// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_it_is_still_fresh : Specification
{
    readonly PendingIssues _pending = new(new ControlledTime(DateTimeOffset.UnixEpoch));
    readonly IssueConsent _scope = new(1, 2, 3);
    readonly PendingIssue _payload = new("Cratis", "Chronicle", "A title", "A body", ["bug"]);
    PendingIssue? _taken;

    async Task Because()
    {
        var token = await _pending.Present(_payload, _scope, _ => Task.FromResult(4UL));
        _taken = _pending.Take(token, _scope, 4);
    }

    [Fact] void should_return_the_exact_immutable_payload() => ReferenceEquals(_taken, _payload).ShouldBeTrue();
}
