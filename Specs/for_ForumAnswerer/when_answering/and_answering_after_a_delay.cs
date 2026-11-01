// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

public class and_answering_after_a_delay : given.a_forum_answerer
{
    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: true);

    [Fact] void should_use_the_delayed_source() => Source().ShouldEqual("discord-forum-delayed");
    [Fact] void should_follow_the_thread() => FollowUps.Accepts(ThreadId).ShouldBeTrue();
    [Fact] void should_record_the_asker_for_follow_ups() => (FollowUps.AskerOf(ThreadId) == Owner).ShouldBeTrue();
    [Fact] void should_still_send_the_follow_up_note() => Contents.Contains(HelpForum.FollowUpNote).ShouldBeTrue();
}
