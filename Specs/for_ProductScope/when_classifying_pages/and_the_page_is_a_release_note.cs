// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_classifying_pages;

public class and_the_page_is_a_release_note : Specification
{
    [Fact] void should_find_the_product() => ProductScope.ProductOf("https://github.com/Cratis/Arc/releases/tag/v1", "https://cratis.io").ShouldEqual(InteractionProduct.Arc);
}
