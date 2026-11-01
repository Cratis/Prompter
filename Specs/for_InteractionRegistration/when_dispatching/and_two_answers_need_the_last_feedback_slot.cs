// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_two_answers_need_the_last_feedback_slot : given.a_button_dispatcher
{
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _competingWrite;

    async Task Because()
    {
        Options.Protection.LocalCapacity = 2;
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        Interactions.RecordFeedback(43, FeedbackVerdict.Up, Arg.Any<CancellationToken>()).Returns(_ => _write.Task);
        var pending = FailingWrite();
        await _started.Task;
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 44));
        _competingWrite = Interactions.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IInteractionLog.RecordFeedback) && (long)call.GetArguments()[0]! == 44);
        _write.SetException(new InvalidOperationException("write failed"));
        await pending;
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 44));
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 44));
    }

    async Task FailingWrite()
    {
        using var turn = await Votes.Enter(43, 7);
        Votes.Check(new(FeedbackVerdict.Up, 43));
        _started.SetResult();
        try
        {
            await Interactions.RecordFeedback(43, FeedbackVerdict.Up);
            Votes.Commit(new(FeedbackVerdict.Up, 43));
        }
        catch (InvalidOperationException)
        {
            // Match the handler's best-effort failed-write path: no commit, then dispose the turn.
        }
    }

    [Fact] void should_refuse_the_competing_write_while_capacity_is_reserved() => _competingWrite.ShouldBeFalse();
    [Fact] void should_record_the_retry_once_after_the_failed_slot_is_released() => Interactions.Received(1).RecordFeedback(44, FeedbackVerdict.Up, Arg.Any<CancellationToken>());
    [Fact] void should_acknowledge_the_successful_votes_repeat_silently() => Payloads[^1].Replace(" ", string.Empty).ShouldContain("\"type\":6");
}
