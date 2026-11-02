// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_AnsweringSwitch.when_refreshing;

public class and_a_poll_started_before_a_pause : Specification
{
    AnsweringSwitch _switch = null!;
    bool _applied;
    bool _paused;
    bool _staleAfterStore;
    bool _stalePersistedPause;

    void Establish() => _switch = Fakes.Switch.Running();

    void Because()
    {
        var started = _switch.Version;
        _switch.BeginPause();
        _switch.PauseStored();
        _applied = _switch.Refresh(false, started);
        _paused = _switch.Paused;

        var resumeStarted = _switch.Version;
        _switch.Resumed();
        _stalePersistedPause = _switch.Refresh(true, resumeStarted);
        _staleAfterStore = _switch.Paused;
    }

    [Fact] void should_discard_the_stale_result() => _applied.ShouldBeFalse();
    [Fact] void should_stay_paused() => _paused.ShouldBeTrue();
    [Fact] void should_discard_a_stale_pause_after_a_resume() => _stalePersistedPause.ShouldBeFalse();
    [Fact] void should_stay_resumed_after_a_stale_pause_read() => _staleAfterStore.ShouldBeFalse();
}
