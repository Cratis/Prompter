// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HelpForum.when_deciding_to_answer;

public class and_the_thread_is_in_another_channel : Specification
{
    const ulong ForumId = 111UL;
    const ulong OtherChannelId = 222UL;
    bool _result;

    void Because() => _result = HelpForum.ShouldAnswer(OtherChannelId, ForumId, authorIsBot: false, "How do I append an event in Chronicle?");

    [Fact] void should_not_answer() => _result.ShouldBeFalse();
}
