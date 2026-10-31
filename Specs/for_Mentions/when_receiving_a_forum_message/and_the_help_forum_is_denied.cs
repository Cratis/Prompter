// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_the_help_forum_is_denied : given.an_admitted_message
{
    void Establish()
    {
        Options.Protection.DeniedChannels = [7];
        FollowUps.Answered(70);
        Message = Build(200, 70, 3, "and what about version 2?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
