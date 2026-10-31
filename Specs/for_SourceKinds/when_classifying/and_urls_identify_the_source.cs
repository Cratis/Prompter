// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SourceKinds.when_classifying;

public class and_urls_identify_the_source : Specification
{
    [Theory]
    [InlineData("https://github.com/Cratis/Chronicle/releases/tag/v1.2.3", SourceKind.ReleaseNotes)]
    [InlineData("https://GITHUB.COM/cratis/Arc/releases/tag/v1", SourceKind.ReleaseNotes)]
    [InlineData("https://github.com/Cratis/Samples/blob/main/README.md", SourceKind.Sample)]
    [InlineData("https://cratis.io/chronicle/index.md", SourceKind.Documentation)]
    [InlineData("https://github.com/Other/Arc/releases/tag/v1", SourceKind.Documentation)]
    [InlineData("https://github.com.evil/Cratis/Arc/releases/tag/v1", SourceKind.Documentation)]
    public void should_classify_the_url(string url, SourceKind expected) => SourceKinds.Of(new PageUrl(url)).ShouldEqual(expected);
}
