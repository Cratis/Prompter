// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_mapping_products;

public class and_the_product_is_a_site_product : Specification
{
    [Theory]
    [InlineData("studio", "https://cratis.studio/")]
    [InlineData("direct", "https://cratis.direct/")]
    [InlineData("assurance", "https://cratis.no/support/")]
    [InlineData("specifications", "https://github.com/Cratis/Specifications/blob/")]
    [InlineData("stage", "https://github.com/Cratis/Stage/blob/")]
    [InlineData("screenplay", "https://github.com/Cratis/Screenplay/blob/")]
    [InlineData("prologue", "https://github.com/Cratis/Prologue/blob/")]
    public void should_parse_the_key_and_include_the_source_prefix(string key, string prefix)
    {
        ProductScope.TryParseKey(key, out var product).ShouldBeTrue();
        ProductScope.PrefixesFor(product, "https://cratis.io").ShouldContain(prefix);
        ProductScope.Key(product).ShouldEqual(key);
    }

    [Fact] void should_include_the_company_studio_page() => ProductScope.PrefixesFor(InteractionProduct.Studio, "https://cratis.io").ShouldContain("https://cratis.no/studio/");
}
