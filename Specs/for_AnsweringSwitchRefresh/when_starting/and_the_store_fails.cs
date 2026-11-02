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
    Exception? _error;
    bool _paused;

    void Establish()
    {
        _switch = Fakes.Switch.Running();
        _switch.Apply(true);
        var pauses = Substitute.For<IAnsweringPauses>();
        pauses.IsPaused(Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ => throw new InvalidOperationException("database down"));
        _refresh = new(_switch, pauses, new Fakes.ControlledTime(DateTimeOffset.UnixEpoch), NullLogger<AnsweringSwitchRefresh>.Instance);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _refresh.StartAsync(CancellationToken.None));
        await _refresh.StopAsync(CancellationToken.None);
        _paused = _switch.Paused;
    }

    [Fact] void should_still_start() => _error.ShouldBeNull();
    [Fact] void should_keep_the_previous_value() => _paused.ShouldBeTrue();
}
