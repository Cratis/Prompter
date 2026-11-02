// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_backticks_cross_markdown_blocks : Specification
{
    [Theory]
    [InlineData("`prefix\n\n[click](https://evil.invalid) @someone\n\nsuffix`")]
    [InlineData("prefix `example\n- [click](https://evil.invalid) @someone\nsuffix`")]
    [InlineData("prefix `example\n> [click](https://evil.invalid) @Cratis/team\nsuffix`")]
    [InlineData("prefix ``example\r\n\r\n[click](https://evil.invalid) @someone\r\nsuffix``")]
    [InlineData("prefix `example\n# [click](https://evil.invalid) @someone\nsuffix`")]
    [InlineData("x ``` y\n[click](https://evil.invalid)")]
    [InlineData("```\nfoo ``` [click](https://evil.invalid)\n```")]
    [InlineData("~~~\n[click](https://evil.invalid) @everyone\n~~~")]
    public void should_defang_regardless_of_fences(string markdown)
    {
        var result = PublishedMarkdown.Neutralize(markdown, new());
        result.ShouldNotContain("https://evil");
        result.ShouldNotContain("@someone");
        result.ShouldNotContain("@everyone");
        result.ShouldNotContain("@Cratis");
        PublishedMarkdown.Neutralize(result, new()).ShouldEqual(result);
    }
}
