// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_holding_a_draft;

public class and_another_is_already_held : Specification
{
    readonly PendingIssues _pending = new(new ControlledTime(DateTimeOffset.UnixEpoch));
    readonly IssueConsent _scope = new(1, 2, 3);
    string _first = null!;
    string _second = null!;

    async Task Because()
    {
        _first = await _pending.Present(new("Cratis", "Chronicle", "First", "Body", []), _scope, _ => Task.FromResult(4UL));
        _second = await _pending.Present(new("Cratis", "Chronicle", "Second", "Body", []), _scope, _ => Task.FromResult(5UL));
    }

    [Fact] void should_give_each_its_own_token() => _second.ShouldNotEqual(_first);
    [Fact] void should_use_256_random_bits() => Convert.FromHexString(_first).Length.ShouldEqual(32);
    [Fact] void should_keep_them_apart() => _pending.Take(_first, _scope, 4)!.Title.ShouldEqual("First");
}
