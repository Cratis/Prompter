// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ProductScope.when_mapping_products;

public class and_a_product_is_known : Specification
{
    IReadOnlyList<string> _prefixes = [];

    void Because() => _prefixes = ProductScope.PrefixesFor(InteractionProduct.Cli, "https://cratis.io/");

    [Fact] void should_include_the_documentation_prefix() => _prefixes.ShouldContain("https://cratis.io/cli/");
    [Fact] void should_include_the_release_notes_prefix() => _prefixes.ShouldContain("https://github.com/Cratis/cli/releases/");
    [Fact] void should_display_the_cli_in_capitals() => ProductScope.Display(InteractionProduct.Cli).ShouldEqual("CLI");
    [Fact] void should_round_trip_the_key() => (ProductScope.TryParseKey(ProductScope.Key(InteractionProduct.Arc), out var product) && product == InteractionProduct.Arc).ShouldBeTrue();
}
