// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_product_was_not_recognized : Specification
{
    string _preview = null!;

    void Because() => _preview = IssuePreview.Text(new("Cratis", "Documentation", "Title", "Body", []), routed: false)!;

    [Fact] void should_call_out_the_fallback() => _preview.ShouldContain("couldn't tell which product");
    [Fact] void should_show_the_actual_destination() => _preview.ShouldContain("Cratis/Documentation");
}
