// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_defining_what_cratis_is : Specification
{
    [Fact] public void should_require_leading_with_the_documented_terms_for_identity_questions() =>
        SystemPrompt.Text.ShouldContain("For identity/definition questions asking what Cratis is");
    [Fact] public void should_name_event_sourcing_and_cqrs_as_examples() =>
        SystemPrompt.Text.ShouldContain("\"event sourcing\" and \"CQRS\"");
    [Fact] public void should_forbid_a_product_list_without_naming_the_kind_of_system() =>
        SystemPrompt.Text.ShouldContain("is an incomplete definition");
}
