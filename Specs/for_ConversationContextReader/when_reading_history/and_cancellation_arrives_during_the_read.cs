// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_cancellation_arrives_during_the_read : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(async _ =>
    {
        await cancellation.CancelAsync();
        return await Task.FromCanceled<IReadOnlyList<HistoryMessage>>(cancellation.Token);
    });

    async Task Because() => result = await Read();

    [Fact] void should_keep_the_current_question() => result.Value.ShouldEqual("What about that API?");
    [Fact] void should_discard_history() => result.Conversation.Messages.ShouldBeEmpty();
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
