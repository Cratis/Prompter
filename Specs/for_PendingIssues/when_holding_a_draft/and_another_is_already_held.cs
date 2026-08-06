// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_holding_a_draft;

public class and_another_is_already_held : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.UnixEpoch);
    PendingIssues _pending = null!;
    string _first = null!;
    string _second = null!;

    void Establish() => _pending = new(_time);

    void Because()
    {
        _first = _pending.Hold(new("First", "A body", IssueKind.Bug, "chronicle"), "Chronicle", null);
        _second = _pending.Hold(new("Second", "A body", IssueKind.Bug, "chronicle"), "Chronicle", null);
    }

    [Fact] void should_give_each_its_own_token() => _second.ShouldNotEqual(_first);
    [Fact] void should_keep_them_apart() => _pending.Take(_first)!.Draft.Title.ShouldEqual("First");
}
