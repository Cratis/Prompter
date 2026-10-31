// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SampleAllowlist.when_matching;

public class and_paths_are_curated : Specification
{
    [Theory]
    [InlineData("LearningPath/One/Command.cs", true)]
    [InlineData("LearningPath/README.md", true)]
    [InlineData("README.md", true)]
    [InlineData("Other/Command.cs", false)]
    [InlineData("LearningPath/.cratis/Command.cs", false)]
    [InlineData("LearningPath/bin/Command.cs", false)]
    [InlineData("LearningPath/obj/Command.cs", false)]
    [InlineData("LearningPath/node_modules/Command.cs", false)]
    [InlineData("LearningPath/A.g.cs", false)]
    [InlineData("LearningPath/A.Designer.cs", false)]
    [InlineData("LearningPath/Generated/A.cs", false)]
    [InlineData("learningpath/A.cs", false)]
    public void should_match_only_curated_paths(string path, bool allowed) =>
        SampleAllowlist.Allows(path, new SamplesOptions().Paths, 100, 16_384).ShouldEqual(allowed);

    [Theory]
    [InlineData("appsettings.Development.json")]
    [InlineData("local.env")]
    [InlineData(".env.local")]
    [InlineData("cert.pfx")]
    [InlineData("cert.pem")]
    [InlineData("cert.key")]
    [InlineData("cert.snk")]
    public void should_exclude_sensitive_files_even_with_a_wildcard(string path) => SampleAllowlist.Allows(path, ["**"], 10, 100).ShouldBeFalse();

    [Fact] void should_enforce_the_size_cap() => SampleAllowlist.Allows("README.md", ["**"], 101, 100).ShouldBeFalse();
    [Fact] void should_allow_the_exact_size_cap() => SampleAllowlist.Allows("README.md", ["**"], 100, 100).ShouldBeTrue();
}
