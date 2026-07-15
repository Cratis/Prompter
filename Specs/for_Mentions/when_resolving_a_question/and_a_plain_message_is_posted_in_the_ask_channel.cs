// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_Mentions.when_resolving_a_question;

public class and_a_plain_message_is_posted_in_the_ask_channel : Specification
{
    const ulong BotId = 42UL;
    const ulong AskChannelId = 555UL;
    (bool ShouldAnswer, string Question) _result;

    void Because() => _result = Mentions.ResolveQuestion("How do I project a read model?", BotId, authorIsBot: false, AskChannelId, AskChannelId);

    [Fact] void should_answer() => _result.ShouldAnswer.ShouldBeTrue();
    [Fact] void should_treat_the_whole_message_as_the_question() => _result.Question.ShouldEqual("How do I project a read model?");
}
