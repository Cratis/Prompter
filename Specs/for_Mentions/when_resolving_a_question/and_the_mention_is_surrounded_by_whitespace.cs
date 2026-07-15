// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_Mentions.when_resolving_a_question;

public class and_the_mention_is_surrounded_by_whitespace : Specification
{
    const ulong BotId = 42UL;
    const ulong ChannelId = 999UL;
    const ulong AskChannelId = 555UL;
    (bool ShouldAnswer, string Question) _result;

    void Because() => _result = Mentions.ResolveQuestion($"  <@{BotId}>   What is Chronicle?  ", BotId, authorIsBot: false, ChannelId, AskChannelId);

    [Fact] void should_answer() => _result.ShouldAnswer.ShouldBeTrue();
    [Fact] void should_yield_the_trimmed_question() => _result.Question.ShouldEqual("What is Chronicle?");
}
