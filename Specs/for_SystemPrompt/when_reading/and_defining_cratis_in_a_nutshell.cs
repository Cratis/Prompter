// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_defining_cratis_in_a_nutshell : Specification
{
    [Fact] public void should_require_the_defining_architecture_before_any_product() =>
        SystemPrompt.Text.ShouldContain("open with the architecture");
    [Fact] public void should_name_event_sourcing_as_the_example_definition() =>
        SystemPrompt.Text.ShouldContain("for example event sourcing");
    [Fact] public void should_require_it_stated_before_naming_any_product() =>
        SystemPrompt.Text.ShouldContain("before naming any product");
    [Fact] public void should_reject_a_product_list_as_a_definition_on_its_own() =>
        SystemPrompt.Text.ShouldContain("A product-by-product list alone is not a definition");
}
