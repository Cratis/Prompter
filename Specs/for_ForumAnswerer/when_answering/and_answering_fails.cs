// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

public class and_answering_fails : given.a_forum_answerer
{
    void Establish() => Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Answer>(new InvalidOperationException()));

    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: false);

    [Fact] void should_send_only_the_error_reply() => Contents.SequenceEqual([Options.Discord.ErrorReply]).ShouldBeTrue();
    [Fact] void should_mention_nobody() => Rest.MentionedNobody.ShouldBeTrue();
}
