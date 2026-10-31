// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_the_thread_is_denied : given.delayed_forum_answers
{
    void Establish()
    {
        Post(70, 45);
        Options.Protection.DeniedChannels = [70];
    }

    async Task Because() => Answered = await Sweeper.SweepOnce(CancellationToken.None);

    [Fact] void should_answer_nothing() => Answered.ShouldEqual(0);
    [Fact] void should_not_read_its_messages() => Threads.DidNotReceive().Recent(70, Arg.Any<int>(), Arg.Any<CancellationToken>());
}
