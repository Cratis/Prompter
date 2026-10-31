// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_only_semantic_retrieval_finds_documentation : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.8));
    async Task Because() => result = await answers.For(new("What is Cratis Chronicle, according to the public documentation?"), "cli");

    [Fact] void should_answer_despite_a_single_arm_rank() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_invoke_chat_once() => chat.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_include_the_documentation() => sent[1].Text.ShouldContain(Documentation);
    [Fact] void should_preserve_provenance() => result.Passages.Single().ShouldEqual(Passage(0.8));
    [Fact] void should_cite_the_original_page() => result.Citations.Single().ShouldEqual(Passage(0.8).Page);
    [Fact] void should_report_similarity_not_rank() => result.Confidence.ShouldEqual(0.8);
    [Fact] void should_exit_successfully() => AskOutput.ExitCode(result).ShouldEqual(0);
    [Fact] void should_record_once() => interactions.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_record_the_same_similarity() => ((Interaction)interactions.ReceivedCalls().Single().GetArguments()[0]!).Confidence.ShouldEqual(0.8);
    [Fact] void should_attach_the_interaction_id() => result.InteractionId.ShouldEqual(42L);
}
