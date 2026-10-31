// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_Mentions.when_resolving_a_question;

public class and_an_addressed_message_is_written_by_a_bot : Specification
{
    const ulong BotId = 42UL;
    (bool ShouldAnswer, string Question) _result;

    void Because() => _result = Mentions.ResolveQuestion("What about version 2?", BotId, authorIsBot: true, 999UL, 555UL, addressed: true);

    [Fact] void should_not_answer() => _result.ShouldAnswer.ShouldBeFalse();
}
