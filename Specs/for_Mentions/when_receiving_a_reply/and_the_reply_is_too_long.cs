// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_the_reply_is_too_long : given.an_admitted_message
{
    void Establish() => Message = Build(
        100,
        5,
        3,
        new('x', Math.Min(Options.Protection.MaxQuestionCharacters, ConversationContextReader.MaximumCurrentCharacters) + 1),
        referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true));

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_reserve_spend() => Ledger.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
