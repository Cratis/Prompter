// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class the_total_does_not_match : given.durable_accounting
{
    async Task Because()
    {
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        await Send(Dispatch(), new() { InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 999 });
    }

    [Fact] void should_retain_the_bound() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), null, Arg.Any<CancellationToken>());
    [Fact] void should_log_the_mismatch() => Rejections().ShouldEqual([nameof(SettlementRejection.TotalMismatch)]);
}
