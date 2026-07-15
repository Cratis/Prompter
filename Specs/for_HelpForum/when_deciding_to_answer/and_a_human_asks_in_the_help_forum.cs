// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HelpForum.when_deciding_to_answer;

public class and_a_human_asks_in_the_help_forum : Specification
{
    const ulong ForumId = 111UL;
    bool _result;

    void Because() => _result = HelpForum.ShouldAnswer(ForumId, ForumId, authorIsBot: false, "How do I append an event in Chronicle?");

    [Fact] void should_answer() => _result.ShouldBeTrue();
}
