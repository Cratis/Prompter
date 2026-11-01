// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_replies;

public class and_the_thread_is_busy : Specification
{
    const ulong ThreadId = 100;
    ForumReplyState _result;

    void Because() => _result = DelayedForumSchedule.Replies([.. Enumerable.Range(0, DelayedForumSchedule.ReplyScanLimit).Select(index => new ForumMessage(100UL + (ulong)index, 3, false, false, true))], ThreadId, 3, 42);

    [Fact] void should_report_busy() => _result.ShouldEqual(ForumReplyState.Busy);
}
