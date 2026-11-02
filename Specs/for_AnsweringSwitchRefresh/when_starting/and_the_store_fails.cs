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
    AnsweringSwitch _switch = null!;
    AnsweringSwitchRefresh _refresh = null!;
    IAnsweringPauses _pauses = null!;
    Exception? _error;
    bool _closedAfterFailure;
    bool _awaiting;
    bool _openAfterRecovery;

    void Establish()
    {
        _switch = new(NullLogger<AnsweringSwitch>.Instance);
        _pauses = Substitute.For<IAnsweringPauses>();
        _pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<bool>(new InvalidOperationException("database down")),
            _ => Task.FromResult(false));
        _refresh = new(_switch, _pauses, new Fakes.ControlledTime(DateTimeOffset.UnixEpoch), NullLogger<AnsweringSwitchRefresh>.Instance);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _refresh.StartAsync(CancellationToken.None));
        _closedAfterFailure = _switch.Paused;
        _awaiting = _switch.AwaitingFirstRead;
        await _refresh.StopAsync(CancellationToken.None);
        var started = _switch.Version;
        _switch.Refresh(await _pauses.IsPaused(default), started);
        _openAfterRecovery = !_switch.Paused;
    }

    [Fact] void should_still_start() => _error.ShouldBeNull();
    [Fact] void should_keep_answering_closed_after_a_failed_first_read() => _closedAfterFailure.ShouldBeTrue();
    [Fact] void should_report_that_it_awaits_the_first_read() => _awaiting.ShouldBeTrue();
    [Fact] void should_open_after_a_successful_read() => _openAfterRecovery.ShouldBeTrue();
}
