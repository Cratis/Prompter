// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_answering_definition_questions : Specification
{
    [Fact] void should_ask_for_a_definition_first() => SystemPrompt.Text.ShouldContain("For definition questions about Cratis itself");
    [Fact] void should_keep_the_defining_terms_verbatim() => SystemPrompt.Text.ShouldContain("word for word");
    [Fact] void should_still_forbid_meta_talk() => SystemPrompt.Text.ShouldContain("Never mention excerpts");
}
