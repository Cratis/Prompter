// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_channel_is_not_public_for_a_reply : given.a_history_reader
{
    void Establish() => history.IsPublic(location, Arg.Any<CancellationToken>()).Returns(false);

    async Task Because() => result = await ReadReplyingTo(Prompter(40, "Which version are you on?", Now.AddMinutes(-40)));

    [Fact] void should_degrade_to_the_current_question() => result.Conversation.IsDegraded.ShouldBeTrue();
    [Fact] void should_not_include_the_replied_message() => result.Conversation.Messages.ShouldBeEmpty();
}
