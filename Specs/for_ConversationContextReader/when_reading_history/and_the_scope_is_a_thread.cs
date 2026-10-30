// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_scope_is_a_thread : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>([Human(50)]));

    async Task Because() => result = await Read();

    [Fact] void should_use_only_the_exact_thread_and_exclusive_trigger() => history.ReceivedCalls().Select(call => (ConversationLocation)call.GetArguments()[0]!).ShouldEqual([location, location]);
    [Fact] void should_not_serialize_transport_metadata() => System.Text.Json.JsonSerializer.Serialize(result.Conversation).Contains("ChannelId", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_persist_history_with_the_question() => System.Text.Json.JsonSerializer.Serialize(result).Contains("Earlier question", StringComparison.Ordinal).ShouldBeFalse();
}
