// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_mapping_products;

public class and_the_product_is_unknown : Specification
{
    [Fact] void should_have_no_prefixes() => ProductScope.PrefixesFor(InteractionProduct.OtherOrUnknown, "https://cratis.io").ShouldBeEmpty();
    [Fact] void should_not_parse_an_unknown_key() => ProductScope.TryParseKey("nothing", out _).ShouldBeFalse();
}
