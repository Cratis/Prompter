// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueComposition.when_building_a_body;

public class and_the_conversation_is_linkable : Specification
{
    const string Url = "https://discord.com/channels/1/2";
    string _body = null!;

    void Because() => _body = IssueComposition.Body(new("Title", "The description.", IssueKind.Bug, "chronicle"), Url);

    [Fact] void should_keep_the_drafted_body() => _body.StartsWith("The description.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_where_it_came_from() => _body.ShouldContain(IssueComposition.ContextHeading);
    [Fact] void should_link_back_to_the_conversation() => _body.ShouldContain(Url);
    [Fact] void should_name_prompter_as_the_filer() => _body.ShouldContain("Prompter");
}
