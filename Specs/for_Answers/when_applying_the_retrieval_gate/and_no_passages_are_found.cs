// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_no_passages_are_found : given.a_grounded_answerer
{
    void Establish() => Found();
    async Task Because() => result = await answers.For(new("What is Cratis Chronicle?"), "cli");

    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_invoke_chat() => chat.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_report_zero_similarity() => result.Confidence.ShouldEqual(0);
    [Fact] void should_have_no_citations() => result.Citations.Any().ShouldBeFalse();
    [Fact] void should_record_once() => interactions.ReceivedCalls().Count().ShouldEqual(1);
}
