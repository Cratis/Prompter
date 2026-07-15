// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;

namespace Cratis.Prompter.Specs.for_ReindexGate.when_exiting;

public class and_a_completed_run_is_released : Specification
{
    readonly ReindexGate _gate = new();
    bool _reentered;

    void Establish()
    {
        _gate.TryEnter();
        _gate.Exit();
    }

    void Because() => _reentered = _gate.TryEnter();

    [Fact] void should_admit_a_new_run() => _reentered.ShouldBeTrue();
}
