// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_BoundedWork.when_reserving;

public class and_callers_arrive_together : Specification
{
    readonly ConcurrentBag<WorkTurn> _turns = [];
    BoundedWork _work = null!;
    bool _capacityReleased;

    void Establish() => _work = new(2, 100, TimeProvider.System);

    void Because()
    {
        Parallel.For(0, 50, _ =>
        {
            if (_work.TryEnter() is { } turn) _turns.Add(turn);
        });
        foreach (var turn in _turns)
        {
            turn.Dispose();
            turn.Dispose();
        }
        using var first = _work.TryEnter();
        using var second = _work.TryEnter();
        using var third = _work.TryEnter();
        _capacityReleased = first is not null && second is not null && third is null;
    }

    [Fact] void should_reserve_only_running_capacity() => _turns.Count.ShouldEqual(2);
    [Fact] void should_release_each_turn_only_once() => _capacityReleased.ShouldBeTrue();
}
