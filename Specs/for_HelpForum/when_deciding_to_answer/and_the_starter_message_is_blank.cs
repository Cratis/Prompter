// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HelpForum.when_deciding_to_answer;

public class and_the_starter_message_is_blank : Specification
{
    const ulong ForumId = 111UL;
    bool _result;

    void Because() => _result = HelpForum.ShouldAnswer(ForumId, ForumId, authorIsBot: false, "   ");

    [Fact] void should_not_answer() => _result.ShouldBeFalse();
}
