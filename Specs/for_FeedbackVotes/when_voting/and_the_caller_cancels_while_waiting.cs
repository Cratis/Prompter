// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_the_caller_cancels_while_waiting : given.a_feedback_budget
{
    FeedbackTurn? _canceled;
    FeedbackTurn? _retry;

    void Establish() => Create();

    async Task Because()
    {
        var first = await Votes.Enter(1, 1);
        using var caller = new CancellationTokenSource();
        var pending = Votes.Enter(1, 2, caller.Token);
        await caller.CancelAsync();
        _canceled = await pending;
        first!.Dispose();
        _retry = await Votes.Enter(1, 2);
        _retry?.Dispose();
    }

    [Fact] void should_release_the_canceled_waiter() => _canceled.ShouldBeNull();
    [Fact] void should_allow_another_turn() => _retry.ShouldNotBeNull();
}
