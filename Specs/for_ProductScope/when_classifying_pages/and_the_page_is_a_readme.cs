// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_classifying_pages;

public class and_the_page_is_a_readme : Specification
{
    [Fact] void should_map_the_chronicle_readme() => ProductScope.ProductOf("https://github.com/Cratis/Chronicle/blob/main/README.md", "https://cratis.io").ShouldEqual(InteractionProduct.Chronicle);
    [Fact] void should_map_the_direct_home_page() => ProductScope.ProductOf("https://cratis.direct/", "https://cratis.io").ShouldEqual(InteractionProduct.Direct);
}
