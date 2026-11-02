// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_BoundedWork.when_reserving;

public class and_the_window_expires : Specification
{
    bool _throughputExhausted;
    bool _runningStillCounted;
    bool _refilled;
    bool _zeroDenies;

    void Because()
    {
        var clock = new ControlledTime(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var work = new BoundedWork(1, 1, clock);
        var first = work.TryEnter()!;
        clock.Advance(TimeSpan.FromSeconds(60));
        _runningStillCounted = work.TryEnter() is null;
        first.Dispose();
        using var second = work.TryEnter();
        _refilled = second is not null;
        second!.Dispose();
        _throughputExhausted = work.TryEnter() is null;
        _zeroDenies = new BoundedWork(1, 0, clock).TryEnter() is null;
    }

    [Fact] void should_expire_throughput_without_releasing_running_work() => _runningStillCounted.ShouldBeTrue();
    [Fact] void should_refill_at_sixty_seconds() => _refilled.ShouldBeTrue();
    [Fact] void should_not_refund_throughput_on_disposal() => _throughputExhausted.ShouldBeTrue();
    [Fact] void should_treat_zero_as_denial() => _zeroDenies.ShouldBeTrue();
}
