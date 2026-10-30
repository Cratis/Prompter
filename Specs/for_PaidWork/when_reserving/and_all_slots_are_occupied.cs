// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_all_slots_are_occupied : given.a_paid_work
{
    Exception? _error;
    int _reservations;

    async Task Because()
    {
        await using var first = await Work.BeginInteractive();
        await using var second = await Work.BeginInteractive();
        await using var third = await Work.BeginInteractive();
        _error = await Catch.Exception(async () =>
        {
            await using var fourth = await Work.BeginInteractive();
        });
        _reservations = Ledger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ISpendLedger.Reserve));
    }

    [Fact] void should_refuse_without_queuing() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_reserve_for_the_rejected_operation() => _reservations.ShouldEqual(3);
}
