// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_replies;

public class and_another_bot_posted : Specification
{
    const ulong ThreadId = 100;
    const ulong Owner = 3;
    const ulong Bot = 42;
    ForumReplyState _result;

    void Because() => _result = DelayedForumSchedule.Replies([new(100, Owner, false, false, true), new(101, 8, true, false, true), new(102, 7, false, true, true), new(103, 6, false, false, false)], ThreadId, Owner, Bot);

    [Fact] void should_report_unanswered() => _result.ShouldEqual(ForumReplyState.Unanswered);
}
