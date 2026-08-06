// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_holding_many_drafts;

public class and_the_capacity_is_exceeded : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.UnixEpoch);
    PendingIssues _pending = null!;
    string _first = null!;
    string _last = null!;

    void Establish() => _pending = new(_time);

    void Because()
    {
        _first = _pending.Hold(new("First", "A body", IssueKind.Bug, "chronicle"), "Chronicle", null);

        for (var index = 0; index < PendingIssues.Capacity; index++)
        {
            _last = _pending.Hold(new("Another", "A body", IssueKind.Bug, "chronicle"), "Chronicle", null);
        }
    }

    [Fact] void should_drop_the_oldest() => (_pending.Take(_first) is null).ShouldBeTrue();
    [Fact] void should_keep_the_newest() => (_pending.Take(_last) is not null).ShouldBeTrue();
}
