// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_WorkLease.when_settling;

public class a_canceled_request : for_PaidWork.given.a_paid_work
{
    async Task Because()
    {
        using var canceled = new CancellationTokenSource();
        await using var lease = await Work.BeginInteractive(canceled.Token);
        lease.Activate();
        lease.DispatchChat();
        await canceled.CancelAsync();
    }

    [Fact] void should_settle_with_an_independent_live_deadline() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), Arg.Any<SpendExposure>(), Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
}
