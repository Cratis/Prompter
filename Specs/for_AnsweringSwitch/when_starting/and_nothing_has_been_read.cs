// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_AnsweringSwitch.when_starting;

public class and_nothing_has_been_read : Specification
{
    AnsweringSwitch _switch = null!;
    bool _initial;
    bool _afterRead;

    void Because()
    {
        _switch = new(NullLogger<AnsweringSwitch>.Instance);
        _initial = _switch.Paused;
        _switch.Refresh(false, _switch.Version);
        _afterRead = _switch.Paused;
    }

    [Fact] void should_be_closed() => _initial.ShouldBeTrue();
    [Fact] void should_open_after_the_first_successful_read() => _afterRead.ShouldBeFalse();
}
