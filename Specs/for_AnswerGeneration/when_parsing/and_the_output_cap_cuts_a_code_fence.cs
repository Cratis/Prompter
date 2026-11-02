// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_output_cap_cuts_a_code_fence : Specification
{
    const string Lead = "{\"text\":\"Append an event with the event log [1].\\n\\n```csharp\\nawait eventLog.Append(id, new Registered());\\n";

    [Fact] void should_drop_the_open_fence_and_everything_after() => AnswerGeneration.Parse(Lead + "// done.\\nvar x")!.Text.ShouldEqual("Append an event with the event log [1].\n…");
    [Fact] void should_keep_a_closed_fence() => AnswerGeneration.Parse(Lead + "```\\nThen proj")!.Text.ShouldEqual("Append an event with the event log [1].\n\n```csharp\nawait eventLog.Append(id, new Registered());\n```\n…");
    [Fact] void should_use_the_fallback_when_the_answer_starts_with_an_open_fence() => AnswerGeneration.Parse("{\"text\":\"```csharp\\nvar x = 1; // [1].\\nvar y").ShouldBeNull();
}
