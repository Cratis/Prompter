// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

public class and_answering_immediately : given.a_forum_answerer
{
    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: false);

    [Fact] void should_use_the_immediate_source() => Source().ShouldEqual("discord-forum");
    [Fact] void should_follow_the_thread() => FollowUps.Accepts(ThreadId).ShouldBeTrue();
    [Fact] void should_not_record_the_asker() => FollowUps.AskerOf(ThreadId).ShouldBeNull();
}
