// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_Mentions.when_resolving_a_question;

public class and_the_bot_is_mentioned_by_nickname : Specification
{
    const ulong BotId = 42UL;
    const ulong ChannelId = 999UL;
    const ulong AskChannelId = 555UL;
    (bool ShouldAnswer, string Question) _result;

    void Because() => _result = Mentions.ResolveQuestion($"<@!{BotId}> How do I append an event in Chronicle?", BotId, authorIsBot: false, ChannelId, AskChannelId);

    [Fact] void should_answer() => _result.ShouldAnswer.ShouldBeTrue();
    [Fact] void should_strip_the_nickname_mention_from_the_question() => _result.Question.ShouldEqual("How do I append an event in Chronicle?");
}
