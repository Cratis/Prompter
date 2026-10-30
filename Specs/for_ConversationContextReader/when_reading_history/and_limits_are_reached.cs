// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_limits_are_reached : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>(
        Enumerable.Range(1, 12).Select(index => Human((ulong)index, new string('x', 1000))).ToArray()));

    async Task Because() => result = await Read(new string('q', 4000));

    [Fact] void should_bound_history_to_six_thousand_characters() => result.Conversation.Messages.Sum(message => message.Content.Length).ShouldEqual(6000);
    [Fact] void should_prefer_recent_history() => result.Conversation.Messages.Count.ShouldEqual(6);
    [Fact] void should_keep_the_current_message_separately() => result.Value.Length.ShouldEqual(4000);
}
