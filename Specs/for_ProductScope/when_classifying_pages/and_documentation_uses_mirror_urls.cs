// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_classifying_pages;

public class and_documentation_uses_mirror_urls : Specification
{
    [Theory]
    [InlineData("https://cratis.io/arc.md", "https://cratis.io")]
    [InlineData("https://www.cratis.io/arc.md", "https://cratis.io")]
    [InlineData("https://www.cratis.io/arc/commands.md", "https://cratis.io")]
    [InlineData("https://cratis.io/arc/commands/", "https://www.cratis.io/")]
    [InlineData("https://cratis.io/arc.md", "https://www.cratis.io")]
    void should_identify_arc(string page, string site) => ProductScope.ProductOf(page, site).ShouldEqual(InteractionProduct.Arc);

    [Theory]
    [InlineData("https://cratis.io/arc.md")]
    [InlineData("https://www.cratis.io/arc/")]
    [InlineData("https://www.cratis.io/arc.md")]
    void should_include_the_mirror_prefix(string prefix) => ProductScope.PrefixesFor(InteractionProduct.Arc, "https://cratis.io").ShouldContain(prefix);

    [Fact] void should_not_accept_another_host() => ProductScope.ProductOf("https://example.invalid/arc.md", "https://cratis.io").ShouldBeNull();
    [Fact] void should_not_confuse_a_similar_path() => ProductScope.ProductOf("https://cratis.io/arcade.md", "https://cratis.io").ShouldBeNull();
}
