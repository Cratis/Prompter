// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_the_token_is_unknown : Specification
{
    PendingIssue? _taken;

    void Because() => _taken = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch)).Take("nope");

    [Fact] void should_return_nothing() => (_taken is null).ShouldBeTrue();
}
