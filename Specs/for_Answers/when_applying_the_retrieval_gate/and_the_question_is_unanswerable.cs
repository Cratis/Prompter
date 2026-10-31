// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_the_question_is_unanswerable : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.05, 2.0 / 61));
    async Task Because() => result = await answers.For(new("What are tomorrow's winning lottery numbers?"), "cli");

    [Fact] void should_refuse_even_a_dual_arm_first_rank() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_invoke_chat() => chat.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_cite_unrelated_passages() => result.Citations.Any().ShouldBeFalse();
    [Fact] void should_keep_candidates_for_diagnostics() => result.Passages.Single().ShouldEqual(Passage(0.05, 2.0 / 61));
    [Fact] void should_exit_nonzero() => AskOutput.ExitCode(result).ShouldEqual(1);
    [Fact] void should_record_the_refusal_once() => interactions.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_record_semantic_relevance_not_rank() => ((Interaction)interactions.ReceivedCalls().Single().GetArguments()[0]!).Confidence.ShouldEqual(0.05);
    [Fact] void should_preserve_the_refusal_text() => result.Text.ShouldEqual(Answer.Refusal(0, []).Text);
}
