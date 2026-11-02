// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_output_cap_cuts_after_a_lead_in : Specification
{
    [Fact] void should_use_the_fallback() => AnswerGeneration.Parse("{\"text\":\"Cratis offers these products:\\n\\n- **Chr").ShouldBeNull();
    [Fact] void should_use_the_fallback_for_complete_lines_without_a_citation() => AnswerGeneration.Parse("{\"text\":\"Cratis has several products.\\n- Chronicle stores events.\\n- Ar").ShouldBeNull();
}
