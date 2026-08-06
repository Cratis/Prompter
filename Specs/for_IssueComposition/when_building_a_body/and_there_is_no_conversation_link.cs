// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueComposition.when_building_a_body;

public class and_there_is_no_conversation_link : Specification
{
    string _body = null!;

    void Because() => _body = IssueComposition.Body(new("Title", "The description.", IssueKind.Bug, "chronicle"), null);

    [Fact] void should_still_say_where_it_came_from() => _body.ShouldContain(IssueComposition.ContextHeading);
    [Fact] void should_not_leave_an_empty_link() => _body.Contains("[the conversation]", StringComparison.Ordinal).ShouldBeFalse();
}
