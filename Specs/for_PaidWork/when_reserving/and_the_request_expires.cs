// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_the_request_expires : given.a_paid_work
{
    bool _canceled;

    async Task Because()
    {
        using var caller = new CancellationTokenSource();
        await using var lease = await Work.BeginInteractive(caller.Token);
        await caller.CancelAsync();
        _canceled = lease.Token.IsCancellationRequested;
    }

    [Fact] void should_propagate_cancellation_to_every_dependency() => _canceled.ShouldBeTrue();
}
