// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class usage_is_not_fully_known : given.durable_accounting
{
    [Theory]
    [InlineData(-1, 1, false)]
    [InlineData(1, -1, false)]
    [InlineData(32001, 1, false)]
    [InlineData(1, 4097, false)]
    [InlineData(1, 1, true)]
    public async Task should_retain_the_bound(long input, long output, bool cached)
    {
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        var usage = new UsageDetails { InputTokenCount = input, OutputTokenCount = output };
        if (cached)
        {
            usage.AdditionalCounts = new() { ["cache_read"] = 1 };
        }

        await Send(Dispatch(), usage);
        await Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), null, Arg.Any<CancellationToken>());
    }
}
