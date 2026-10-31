// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_the_caller_does_not_opt_in : given.a_grounded_answerer
{
    void Establish() => Found(
        new Passage(new("https://cratis.io/chronicle/a/"), "T", "H", "c", 0.02, 0.80),
        new Passage(new("https://cratis.io/arc/b/"), "T", "H", "c", 0.01, 0.79));

    async Task Because() => result = await answers.For(new("How do I handle commands?"), "github-webhook", TestContext.Current.CancellationToken);

    [Fact] void should_answer_instead_of_clarifying() => result.Clarification.ShouldBeNull();
    [Fact] void should_generate_an_answer() => sent.Count.ShouldEqual(2);
    [Fact] void should_record_the_interaction() => interactions.Received(1).Record(Arg.Any<Interaction>(), Arg.Any<CancellationToken>());
}
