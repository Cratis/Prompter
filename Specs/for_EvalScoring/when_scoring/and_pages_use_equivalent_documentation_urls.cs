// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_pages_use_equivalent_documentation_urls : Specification
{
    [Theory]
    [InlineData("https://www.cratis.io/arc.md", "https://cratis.io/arc/")]
    [InlineData("http://cratis.io/arc/index.md", "https://www.cratis.io/arc/")]
    [InlineData("https://cratis.io/arc/index.md", "https://www.cratis.io/arc.md")]
    [InlineData("https://www.cratis.io/cratis-stack.md", "https://cratis.io/cratis-stack/")]
    [InlineData("https://www.cratis.io/index.md", "https://cratis.io/")]
    [InlineData("https://CRATIS.io/ARC.MD", "http://www.cratis.io/arc/")]
    void should_match_equivalent_pages(string actual, string expected) => EvalPages.Normalize(actual).ShouldEqual(EvalPages.Normalize(expected));

    [Fact] void should_keep_distinct_hosts_separate() => EvalPages.Normalize("https://other.invalid/arc.md").ShouldNotEqual(EvalPages.Normalize("https://cratis.io/arc/"));
    [Fact] void should_keep_distinct_paths_separate() => EvalPages.Normalize("https://cratis.io/arc.md").ShouldNotEqual(EvalPages.Normalize("https://cratis.io/chronicle/"));
    [Fact] void should_not_strip_an_internal_extension() => EvalPages.Normalize("https://cratis.io/arc.md/commands/").ShouldNotEqual(EvalPages.Normalize("https://cratis.io/arc/commands/"));
}
