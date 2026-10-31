// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

public class and_the_answer_is_delivered : given.a_forum_answerer
{
    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: false);

    [Fact] void should_send_the_answer_and_the_follow_up_note() => Contents.Contains(HelpForum.FollowUpNote).ShouldBeTrue();
    [Fact] void should_send_more_than_one_message() => (Rest.Sent.Count > 1).ShouldBeTrue();
    [Fact] void should_mention_nobody_in_any_message() => Rest.MentionedNobody.ShouldBeTrue();
}
