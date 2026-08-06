// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_body_is_enormous : Specification
{
    string _preview = null!;

    void Because() => _preview = IssuePreview.Text(
        new("A title", new string('x', 5000), IssueKind.Bug, "chronicle"),
        "Chronicle",
        routed: true,
        []);

    [Fact] void should_stay_within_discords_message_limit() => (_preview.Length <= 2000).ShouldBeTrue();
    [Fact] void should_show_that_it_was_shortened() => _preview.ShouldContain("…");
}
