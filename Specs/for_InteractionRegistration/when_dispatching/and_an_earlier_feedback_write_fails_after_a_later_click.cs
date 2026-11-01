// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_an_earlier_feedback_write_fails_after_a_later_click : given.a_button_dispatcher
{
    readonly TaskCompletionSource _upWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _writing = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        Interactions.RecordFeedback(42, FeedbackVerdict.Up, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _writing.SetResult();
            return _upWrite.Task;
        });

        var up = DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
        await _writing.Task;
        var down = DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));

        // Widen the concurrency interleaving while the earlier write remains held by its signal.
        await Task.Yield();
        _upWrite.SetException(new InvalidOperationException("write failed"));
        await Task.WhenAll(up, down);
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));
    }

    [Fact] void should_persist_the_later_verdict_once() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Down, Arg.Any<CancellationToken>());
    [Fact] void should_not_let_the_stale_failure_erase_it_so_a_repeat_is_silent() => Payloads.Count(payload => payload.Replace(" ", string.Empty).Contains("\"type\":6", StringComparison.Ordinal)).ShouldEqual(1);
}
