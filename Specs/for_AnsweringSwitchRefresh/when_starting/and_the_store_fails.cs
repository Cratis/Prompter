// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringSwitchRefresh.when_starting;

public class and_the_store_fails : Specification
{
    readonly TaskCompletionSource _recovered = new();
    AnsweringSwitch _switch = null!;
    AnsweringSwitchRefresh _refresh = null!;
    Fakes.ManualTimeProvider _time = null!;
    Exception? _error;
    bool _closedAfterFailure;
    bool _awaiting;
    bool _openAfterPoll;

    void Establish()
    {
        _switch = new(NullLogger<AnsweringSwitch>.Instance);
        _time = new();
        var pauses = Substitute.For<IAnsweringPauses>();
        pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<bool>(new InvalidOperationException("database down")),
            _ =>
            {
                _recovered.TrySetResult();
                return Task.FromResult(false);
            });
        _refresh = new(_switch, pauses, _time, NullLogger<AnsweringSwitchRefresh>.Instance);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _refresh.StartAsync(CancellationToken.None));
        _closedAfterFailure = _switch.Paused;
        _awaiting = _switch.AwaitingFirstRead;
        while (_time.TimerCount == 0)
        {
            await Task.Yield();
        }

        _time.Tick();
        await _recovered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        while (_switch.AwaitingFirstRead)
        {
            await Task.Yield();
        }

        _openAfterPoll = !_switch.Paused;
        await _refresh.StopAsync(CancellationToken.None);
    }

    [Fact] void should_still_start() => _error.ShouldBeNull();
    [Fact] void should_keep_answering_closed_after_a_failed_first_read() => _closedAfterFailure.ShouldBeTrue();
    [Fact] void should_report_that_it_awaits_the_first_read() => _awaiting.ShouldBeTrue();
    [Fact] void should_open_when_the_next_poll_succeeds() => _openAfterPoll.ShouldBeTrue();
}
