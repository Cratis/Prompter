// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_product_was_recognized : Specification
{
    string _preview = null!;

    void Because() => _preview = IssuePreview.Text(
        new("Projection stops after a rename", "It stops.", IssueKind.Bug, "chronicle"),
        "Chronicle",
        routed: true,
        []);

    [Fact] void should_show_the_title() => _preview.ShouldContain("Projection stops after a rename");
    [Fact] void should_show_the_repository() => _preview.ShouldContain("Chronicle");
    [Fact] void should_show_the_kind() => _preview.ShouldContain("bug");
    [Fact] void should_show_the_body() => _preview.ShouldContain("It stops.");
    [Fact] void should_say_nothing_is_filed_yet() => _preview.ShouldContain("Nothing is filed until you say so.");
    [Fact] void should_not_warn_about_the_product() => _preview.Contains("couldn't tell which product", StringComparison.Ordinal).ShouldBeFalse();
}
