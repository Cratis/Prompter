// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_recommending;

public class and_the_question_is_standard : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.8));

    async Task Because() => result = await answers.For(new("How do projections work?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_not_send_the_mode_line() => sent[1].Text.ShouldNotContain("Answer mode");
    [Fact] void should_stay_standard() => result.Mode.ShouldEqual(AnswerMode.Standard);
}
