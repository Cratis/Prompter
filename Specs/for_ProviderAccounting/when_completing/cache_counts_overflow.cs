// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class cache_counts_overflow : given.durable_accounting
{
    async Task Because()
    {
        Options.Protection.AnthropicAnswer.CacheWriteUsdPerMillion = Options.Protection.AnthropicAnswer.InputUsdPerMillion;
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        await Send(Dispatch(), new()
        {
            InputTokenCount = 1000,
            OutputTokenCount = 20,
            TotalTokenCount = 1020,
            CachedInputTokenCount = long.MaxValue,
            AdditionalCounts = new() { ["CacheCreationInputTokens"] = long.MaxValue }
        });
    }

    [Fact] void should_retain_the_bound() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), null, Arg.Any<CancellationToken>());
    [Fact] void should_log_the_invalid_counts() => Rejections().ShouldEqual([nameof(SettlementRejection.CacheCountsInvalid)]);
}
