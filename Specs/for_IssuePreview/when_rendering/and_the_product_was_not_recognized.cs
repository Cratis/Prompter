// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_product_was_not_recognized : Specification
{
    string _preview = null!;

    void Because() => _preview = IssuePreview.Text(
        new("A title", "A body", IssueKind.Idea, string.Empty),
        "Documentation",
        routed: false,
        []);

    [Fact] void should_warn_that_the_routing_is_a_guess() => _preview.ShouldContain("couldn't tell which product");
}
