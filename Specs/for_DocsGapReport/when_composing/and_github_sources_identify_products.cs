// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_github_sources_identify_products : Specification
{
    [Theory]
    [InlineData("https://github.com/Cratis/Chronicle/releases/tag/v1.2.3", "chronicle")]
    [InlineData("https://GITHUB.COM/cratis/Arc/releases/tag/v1.2.3", "arc")]
    [InlineData("https://github.com/Cratis/Samples/blob/main/README.md", "samples")]
    [InlineData("https://github.com/Cratis/Other/releases/tag/v1", "unknown")]
    [InlineData("https://github.com/Other/Arc/releases/tag/v1", "unknown")]
    public void should_group_by_the_known_repository(string url, string product) =>
        DocsGapReport.Compose([new("discord", [url], true, null, null, null)], default, default).ShouldContain($"**Docs gaps** — {product} 1");
}
