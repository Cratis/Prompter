// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class usage_is_rejected_for_a_reason : given.durable_accounting
{
    [Theory]
    [InlineData(-1, 1, false, SettlementRejection.NoUsage)]
    [InlineData(1, -1, false, SettlementRejection.OutputInvalid)]
    [InlineData(32001, 1, false, SettlementRejection.InputAboveCeiling)]
    [InlineData(1, 4097, false, SettlementRejection.OutputInvalid)]
    [InlineData(1, 1, true, SettlementRejection.UnknownUsageCounter)]
    public async Task should_log_the_structured_reason(long input, long output, bool counter, SettlementRejection reason)
    {
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        var usage = new UsageDetails { InputTokenCount = input, OutputTokenCount = output };
        if (counter)
        {
            usage.AdditionalCounts = new() { ["cache_read"] = 1 };
        }

        await Send(Dispatch(), usage);
        Rejections().ShouldEqual([reason.ToString()]);
    }
}
