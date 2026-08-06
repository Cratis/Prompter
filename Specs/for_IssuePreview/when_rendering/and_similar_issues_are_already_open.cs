// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_similar_issues_are_already_open : Specification
{
    string _preview = null!;

    void Because() => _preview = IssuePreview.Text(
        new("A title", "A body", IssueKind.Bug, "chronicle"),
        "Chronicle",
        routed: true,
        [new(123, "The same thing", "https://github.com/Cratis/Chronicle/issues/123")]);

    [Fact] void should_offer_the_existing_issue() => _preview.ShouldContain("#123 The same thing");
    [Fact] void should_link_it() => _preview.ShouldContain("https://github.com/Cratis/Chronicle/issues/123");
}
