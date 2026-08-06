// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_taking_a_held_draft;

public class and_it_is_still_fresh : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.UnixEpoch);
    PendingIssues _pending = null!;
    PendingIssue _taken = null!;

    void Establish() => _pending = new(_time);

    void Because()
    {
        var token = _pending.Hold(new("A title", "A body", IssueKind.Bug, "chronicle"), "Chronicle", "https://discord.com/x");
        _taken = _pending.Take(token)!;
    }

    [Fact] void should_return_the_draft() => _taken.ShouldNotBeNull();
    [Fact] void should_keep_the_title() => _taken.Draft.Title.ShouldEqual("A title");
    [Fact] void should_keep_the_repository() => _taken.Repository.ShouldEqual("Chronicle");
    [Fact] void should_keep_the_conversation_link() => _taken.ConversationUrl.ShouldEqual("https://discord.com/x");
}
