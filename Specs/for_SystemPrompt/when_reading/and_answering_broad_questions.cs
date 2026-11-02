// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_answering_broad_questions : Specification
{
    [Fact] public void should_forbid_retrieval_meta_talk() => SystemPrompt.Text.ShouldContain("Never mention excerpts");
    [Fact] public void should_ask_for_a_best_supported_partial_answer() => SystemPrompt.Text.ShouldContain("best-supported");
    [Fact] public void should_not_send_the_user_to_read_pages() => SystemPrompt.Text.ShouldContain("Do not send the user to read pages");
    [Fact] public void should_ask_to_name_the_product_behind_each_point() => SystemPrompt.Text.ShouldContain("naming the specific Cratis product");
}
