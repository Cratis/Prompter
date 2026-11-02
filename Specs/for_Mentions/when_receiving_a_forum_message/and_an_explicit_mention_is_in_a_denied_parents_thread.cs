// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_an_explicit_mention_is_in_a_denied_parents_thread : given.an_admitted_message
{
    void Establish()
    {
        Options.Protection.DeniedChannels = [7];
        FollowUps.Answered(70);
        Message = Build(200, 70, 3, "<@42> how do commands work?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
