// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SourceKinds.when_classifying;

public class and_the_url_is_a_readme : Specification
{
    [Theory]
    [InlineData("https://github.com/Cratis/Arc/blob/main/README.md", SourceKind.Readme)]
    [InlineData("https://github.com/Cratis/.github/blob/main/profile/README.md", SourceKind.Readme)]
    [InlineData("https://github.com/Cratis/Samples/blob/main/README.md", SourceKind.Sample)]
    [InlineData("https://github.com/Cratis/Arc/blob/main/nested/README.md", SourceKind.Documentation)]
    [InlineData("https://github.com/Other/Arc/blob/main/README.md", SourceKind.Documentation)]
    public void should_recognize_only_root_or_profile_readmes(string url, SourceKind kind) => SourceKinds.Of(new(url)).ShouldEqual(kind);
}
