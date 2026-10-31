// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_history_arrives_before_the_deadline : given.a_history_reader
{
    readonly TaskCompletionSource<IReadOnlyList<HistoryMessage>> _history = new();

    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(_history.Task);

    async Task Because()
    {
        var reading = Read();
        clock.Advance(TimeSpan.FromMilliseconds(999));
        _history.SetResult([Human(90)]);
        result = await reading;
    }

    [Fact] void should_keep_history() => result.Conversation.Messages.Count.ShouldEqual(1);
    [Fact] void should_not_degrade() => result.Conversation.IsDegraded.ShouldBeFalse();
}
