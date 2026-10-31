// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_Mentions.when_resolving_a_question;

public class and_an_addressed_message_also_mentions_the_bot : Specification
{
    const ulong BotId = 42UL;
    (bool ShouldAnswer, string Question) _result;

    void Because() => _result = Mentions.ResolveQuestion($"<@{BotId}> What about version 2?", BotId, authorIsBot: false, 999UL, 555UL, addressed: true);

    [Fact] void should_answer() => _result.ShouldAnswer.ShouldBeTrue();
    [Fact] void should_strip_the_mention() => _result.Question.ShouldEqual("What about version 2?");
}
