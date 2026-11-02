// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_a_citable_url_embeds_another_destination : Specification
{
    [Theory]
    [InlineData("https://cratis.io/arc/https://evil.invalid")]
    [InlineData("https://cratis.io/arc/?next=www.evil.invalid")]
    [InlineData("https://cratis.io/arc/?next=discord.gg/abc")]
    [InlineData("https://github.com/Cratis/Arc/discord.com/invite/abc")]
    [InlineData("[Docs](https://cratis.io/arc/?u=http://evil.invalid)")]
    public void should_defang_the_whole_token(string markdown)
    {
        var result = PublishedMarkdown.Neutralize(markdown, new());
        result.ShouldNotContain("://evil");
        result.ShouldNotContain("www.evil");
        result.ShouldNotContain("discord.gg");
        result.ShouldNotContain("discord.com/invite");
        result.ShouldNotContain("https://cratis.io");
        result.ShouldNotContain("https://github.com");
        PublishedMarkdown.Neutralize(result, new()).ShouldEqual(result);
    }
}
