// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_holding_many_drafts;

public class and_the_capacity_is_exceeded : Specification
{
    readonly PendingIssues _pending = new(new ControlledTime(DateTimeOffset.UnixEpoch));
    readonly IssueConsent _scope = new(1, 2, 3);
    string _first = null!;
    string _last = null!;

    async Task Because()
    {
        _first = await _pending.Present(new("Cratis", "Chronicle", "First", "Body", []), _scope, _ => Task.FromResult(4UL));
        for (var index = 0; index < PendingIssues.Capacity; index++)
        {
            _last = await _pending.Present(new("Cratis", "Chronicle", "Another", "Body", []), _scope, _ => Task.FromResult(4UL));
        }
    }

    [Fact] void should_drop_the_oldest() => (_pending.Take(_first, _scope, 4) is null).ShouldBeTrue();
    [Fact] void should_keep_the_newest() => (_pending.Take(_last, _scope, 4) is not null).ShouldBeTrue();
}
