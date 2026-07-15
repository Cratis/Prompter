// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;

namespace Cratis.Prompter.Specs.for_ReindexGate.when_entering;

public class and_a_run_is_already_in_progress : Specification
{
    readonly ReindexGate _gate = new();
    bool _first;
    bool _second;

    void Establish() => _first = _gate.TryEnter();

    void Because() => _second = _gate.TryEnter();

    [Fact] void should_admit_the_first_run() => _first.ShouldBeTrue();

    [Fact] void should_reject_the_concurrent_run() => _second.ShouldBeFalse();
}
