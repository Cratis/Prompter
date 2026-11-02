// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_section_expansion_exceeds_the_budget : given.a_grounded_answerer
{
    void Establish()
    {
        options.Protection.AnthropicAnswer.MaxInputTokens = 9000;
        Found(
            Passage(0.9) with { ChunkId = "https://cratis.io/chronicle/#2" },
            Passage(0.1) with { HeadingPath = "Chronicle > Huge", Content = new string('h', 20000), ChunkId = "https://cratis.io/chronicle/#3", IsSectionExpansion = true },
            Passage(0.1) with { HeadingPath = "Chronicle > Small", Content = "Small section text", ChunkId = "https://cratis.io/chronicle/#4", IsSectionExpansion = true });
    }

    async Task Because() => result = await answers.For(new("What is Chronicle good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_skip_the_oversized_row() => sent[1].Text.ShouldNotContain("hhhh");
    [Fact] void should_keep_a_later_small_row() => sent[1].Text.ShouldContain("Small section text");
    [Fact] void should_stay_under_the_input_ceiling() => (sent.Sum(message => Encoding.UTF8.GetByteCount(message.Text)) + 1024 <= options.Protection.AnthropicAnswer.MaxInputTokens).ShouldBeTrue();
}
