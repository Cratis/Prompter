// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_there_are_many_short_messages : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>(
        Enumerable.Range(1, 12).Select(index => Human((ulong)index, $"message {index}")).ToArray()));

    async Task Because() => result = await Read();

    [Fact] void should_keep_at_most_ten_messages() => result.Conversation.Messages.Count.ShouldEqual(10);
    [Fact] void should_keep_the_most_recent_ten_in_order() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(Enumerable.Range(3, 10).Select(index => $"message {index}"));
}
