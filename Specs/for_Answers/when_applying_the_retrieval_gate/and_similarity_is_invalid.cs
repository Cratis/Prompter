// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_similarity_is_invalid : given.a_grounded_answerer
{
    void Establish() => Found(Passage(double.NaN), Passage(double.PositiveInfinity), Passage(double.NegativeInfinity), Passage(1.1), Passage(-1.1));
    async Task Because() => result = await answers.For(new("Does this lexical match establish relevance?"), "cli");

    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_invoke_chat() => chat.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_report_zero_without_valid_candidates() => result.Confidence.ShouldEqual(0);
    [Fact] void should_keep_all_candidates_for_diagnostics() => result.Passages.Count.ShouldEqual(5);
}
