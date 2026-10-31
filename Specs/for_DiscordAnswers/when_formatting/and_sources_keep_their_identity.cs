// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_formatting;

public class and_sources_keep_their_identity : Specification
{
    [Theory]
    [InlineData("https://github.com/Cratis/Arc/releases/tag/v1.2.3", "https://github.com/Cratis/Arc/releases/tag/v1.2.3", " (release notes)")]
    [InlineData("https://github.com/Cratis/Samples/blob/main/README.md", "https://github.com/Cratis/Samples/blob/main/README.md", " (sample)")]
    [InlineData("https://cratis.io/arc.md", "https://cratis.io/arc", "")]
    public void should_render_the_source_link_and_kind(string url, string link, string note)
    {
        var answer = new Answer("Body.", [new PageUrl(url)], 0.9, false, []);
        DiscordAnswers.Format(answer).ShouldEqual($"Body.\n\nSources: <{link}>{note}");
    }

    [Fact]
    void should_prefix_existing_provenance()
    {
        var page = new PageUrl("https://github.com/Cratis/Arc/releases/tag/v1.2.3");
        var answer = new Answer("Body.", [page], 0.9, false, []) { CitationProvenance = [new(page, null, null, "1.2.3")] };
        DiscordAnswers.Format(answer).ShouldContain("(release notes · index date unknown · v1.2.3)");
    }
}
