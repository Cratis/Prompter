// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringSwitchRefresh.when_starting;

public class and_the_store_says_paused : Specification
{
    AnsweringSwitch _switch = null!;
    AnsweringSwitchRefresh _refresh = null!;
    bool _atStart;
    bool _afterResume;

    void Establish()
    {
        _switch = Fakes.Switch.Running();
        var pauses = Substitute.For<IAnsweringPauses>();
        pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(true, false);
        var clock = new Fakes.ControlledTime(DateTimeOffset.UnixEpoch);
        _refresh = new(_switch, pauses, clock, NullLogger<AnsweringSwitchRefresh>.Instance);
    }

    async Task Because()
    {
        await _refresh.StartAsync(CancellationToken.None);
        _atStart = _switch.Paused;
        await Task.Delay(50);
        await _refresh.StopAsync(CancellationToken.None);
        _afterResume = _switch.Paused;
    }

    [Fact] void should_apply_the_persisted_pause_before_serving() => _atStart.ShouldBeTrue();
    [Fact] void should_not_poll_before_the_interval() => _afterResume.ShouldBeTrue();
}
