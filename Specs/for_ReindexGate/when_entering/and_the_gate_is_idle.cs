// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;

namespace Cratis.Prompter.Specs.for_ReindexGate.when_entering;

public class and_the_gate_is_idle : Specification
{
    readonly ReindexGate _gate = new();
    bool _entered;

    void Because() => _entered = _gate.TryEnter();

    [Fact] void should_admit_the_run() => _entered.ShouldBeTrue();
}
