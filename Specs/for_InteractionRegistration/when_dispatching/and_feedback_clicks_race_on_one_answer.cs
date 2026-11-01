// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_feedback_clicks_race_on_one_answer : given.a_button_dispatcher
{
    readonly TaskCompletionSource _upWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _writing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<FeedbackVerdict> _written = [];
    bool _downWaitedForUp;

    async Task Because()
    {
        Interactions.RecordFeedback(42, FeedbackVerdict.Up, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _written.Add(FeedbackVerdict.Up);
            _writing.SetResult();
            return _upWrite.Task;
        });
        Interactions.RecordFeedback(42, FeedbackVerdict.Down, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _written.Add(FeedbackVerdict.Down);
            return Task.CompletedTask;
        });

        var up = DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Up, 42));
        await _writing.Task;
        var down = DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));

        // Widen the concurrency interleaving while the earlier write remains held by its signal.
        await Task.Yield();
        _downWaitedForUp = _written.Count == 1;
        _upWrite.SetResult();
        await Task.WhenAll(up, down);
        await DispatchClick(FeedbackButton.CustomId(FeedbackVerdict.Down, 42));
    }

    [Fact] void should_hold_the_later_click_until_the_earlier_write_finishes() => _downWaitedForUp.ShouldBeTrue();
    [Fact] void should_persist_in_click_order() => string.Join(',', _written).ShouldEqual("Up,Down");
    [Fact] void should_remember_the_last_persisted_verdict() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Down, Arg.Any<CancellationToken>());
}
