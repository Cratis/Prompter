// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_recommending;

public class and_the_question_asks_which_package_to_use : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.8));

    async Task Because() => result = await answers.For(new("Which package should I use for events?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_send_the_mode_line() => sent[1].Text.ShouldContain("Answer mode: recommendation");
    [Fact] void should_mark_the_answer() => result.Mode.ShouldEqual(AnswerMode.Recommendation);
}
