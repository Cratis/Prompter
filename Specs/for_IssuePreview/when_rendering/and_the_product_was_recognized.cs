// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_product_was_recognized : Specification
{
    string _preview = null!;
    PendingIssue _payload = null!;

    void Because()
    {
        _payload = IssueComposition.Payload(new("A title", "A body", IssueKind.Bug, "chronicle"), "Chronicle", "https://discord.com/channels/2/3", new());
        _preview = IssuePreview.Text(_payload)!;
    }

    [Fact] void should_show_the_owner_and_repository() => _preview.ShouldContain("Cratis/Chronicle");
    [Fact] void should_show_the_exact_title() => _preview.ShouldContain(_payload.Title);
    [Fact] void should_show_the_body_and_public_provenance() => _preview.ShouldContain("Filed from Discord by Prompter");
    [Fact] void should_show_every_label() => _preview.ShouldContain("from\\-discord, bug");
    [Fact] void should_not_hide_content_with_markdown() => _preview.ShouldContain("\\[the conversation\\]\\(https://discord\\.com/channels/2/3\\)");
    [Fact] void should_stay_inside_the_encoded_budget() => (_preview.Length <= 1900).ShouldBeTrue();
}
