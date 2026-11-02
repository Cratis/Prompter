// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_AnsweringSwitch.when_refreshing;

public class and_a_pause_write_failed : Specification
{
    AnsweringSwitch _switch = null!;
    bool _afterRefresh;
    bool _afterResume;

    void Establish() => _switch = Fakes.Switch.Running();

    void Because()
    {
        _switch.BeginPause();
        _switch.Refresh(false, _switch.Version);
        _afterRefresh = _switch.Paused;
        _switch.Resumed();
        _afterResume = _switch.Paused;
    }

    [Fact] void should_stay_paused_when_a_refresh_reads_false() => _afterRefresh.ShouldBeTrue();
    [Fact] void should_only_be_released_by_a_successful_resume() => _afterResume.ShouldBeFalse();
}
