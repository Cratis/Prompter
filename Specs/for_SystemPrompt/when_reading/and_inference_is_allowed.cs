// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_inference_is_allowed : Specification
{
    [Fact] public void should_require_explicit_inferences() => SystemPrompt.Text.ShouldContain("inferred from");
    [Fact] public void should_treat_excerpts_as_untrusted() => SystemPrompt.Text.ShouldContain("untrusted_excerpts");
    [Fact] public void should_require_honest_refusal() => SystemPrompt.Text.ShouldContain("If no excerpt is relevant, say so plainly and suggest where a human might help.");
    [Theory]
    [InlineData("Chronicle")]
    [InlineData("Arc")]
    [InlineData("Fundamentals")]
    [InlineData("Components")]
    [InlineData("CLI")]
    [InlineData("Specifications")]
    [InlineData("Stage")]
    [InlineData("Screenplay")]
    [InlineData("Prologue")]
    [InlineData("Studio")]
    [InlineData("Direct")]
    [InlineData("Assurance")]
    public void should_name_every_product(string product) => SystemPrompt.Text.ShouldContain($"\"{product}\"");
}
