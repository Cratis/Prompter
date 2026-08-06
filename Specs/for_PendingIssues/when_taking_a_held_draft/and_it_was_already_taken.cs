// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_it_was_already_taken : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.UnixEpoch);
    PendingIssues _pending = null!;
    PendingIssue? _second;

    void Establish() => _pending = new(_time);

    void Because()
    {
        var token = _pending.Hold(new("A title", "A body", IssueKind.Bug, "chronicle"), "Chronicle", null);
        _pending.Take(token);
        _second = _pending.Take(token);
    }

    [Fact] void should_not_let_a_double_click_file_twice() => (_second is null).ShouldBeTrue();
}
