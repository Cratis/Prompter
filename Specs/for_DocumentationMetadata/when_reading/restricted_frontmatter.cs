// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocumentationMetadata.when_reading;

public class restricted_frontmatter : Specification
{
    static DocumentationMetadata Read(string markdown) => DocumentationMetadata.From(new("https://cratis.io/a.md"), markdown);

    [Theory]
    [InlineData("---\nversion: 1.2.3")]
    [InlineData(" ---\nversion: 1.2.3\n---")]
    [InlineData("---\n version: 1.2.3\n---")]
    [InlineData("---\nversion: 1.2.3\nversion: 1.2.3\n---")]
    [InlineData("---\nversion: '1.2.3\"\n---")]
    void should_not_carry_an_ambiguous_or_malformed_version(string markdown) => Read(markdown).ProductVersion.ShouldBeNull();

    [Fact] void should_unquote_single_quoted_values() => Read("---\r\nversion: '1.2.3'\r\n---").ProductVersion.ShouldEqual("1.2.3");
    [Fact] void should_accept_matching_version_sources() => Read("---\nproductVersion: v1.2.3\nversion: 1.2.3\n---\n# Arc v1.2.3").ProductVersion.ShouldEqual("1.2.3");
    [Fact] void should_ignore_oversized_frontmatter() => Read($"---\nversion: 1.2.3\n{new string(' ', 4096)}\n---").ProductVersion.ShouldBeNull();
    [Fact] void should_skip_tilde_fenced_headings() => Read("~~~\n# Arc v9.9.9\n~~~\n# Arc v1.2.3").ProductVersion.ShouldEqual("1.2.3");
    [Fact] void should_only_consider_the_first_level_one_heading() => Read("# Guide\n# Arc v1.2.3").ProductVersion.ShouldBeNull();
}
