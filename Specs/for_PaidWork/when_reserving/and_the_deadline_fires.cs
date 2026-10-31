// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_the_deadline_fires : given.a_paid_work
{
    bool _canceled;
    TimeSpan _dueTime;

    async Task Because()
    {
        var clock = new DeadlineClock();
        using var work = new PaidWork(Ledger, Microsoft.Extensions.Options.Options.Create(Options), clock);
        await using var lease = await work.BeginInteractive();
        _dueTime = clock.DueTime;
        clock.Expire();
        _canceled = lease.Token.IsCancellationRequested;
    }

    [Fact] void should_bound_the_entire_operation_at_sixty_seconds() => _dueTime.ShouldEqual(TimeSpan.FromSeconds(60));
    [Fact] void should_cancel_downstream_work() => _canceled.ShouldBeTrue();
}
