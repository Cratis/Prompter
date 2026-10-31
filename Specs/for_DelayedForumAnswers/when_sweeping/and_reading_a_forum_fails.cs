// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_reading_a_forum_fails : given.delayed_forum_answers
{
    const ulong OtherForum = 43;
    Exception? _error;

    void Establish()
    {
        Options.Discord.DelayedForums = [new() { ChannelId = Forum }, new() { ChannelId = OtherForum }];
        Threads.Active(Forum, Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<ForumThreadSnapshot>>>(_ => throw new InvalidOperationException());
        Post(80, 45, forum: OtherForum);
    }

    async Task Because() => _error = await Catch.Exception(async () => Answered = await Sweeper.SweepOnce(CancellationToken.None));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_still_answer_the_other_forum() => Answered.ShouldEqual(1);
}
