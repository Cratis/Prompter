// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class cache_write_usage_is_priced : given.durable_accounting
{
    decimal _expected;

    async Task Because()
    {
        var prices = Options.Protection.AnthropicAnswer;
        prices.InputUsdPerMillion = 0.25m;
        prices.CacheWriteUsdPerMillion = 0.25m;
        prices.CacheReadUsdPerMillion = 0.02m;
        _expected = ((100 * prices.InputUsdPerMillion) + (600 * prices.CacheWriteUsdPerMillion) + (300 * prices.CacheReadUsdPerMillion) + (20 * prices.OutputUsdPerMillion)) / 1000000m;
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        await Send(Dispatch(), new()
        {
            InputTokenCount = 1000,
            OutputTokenCount = 20,
            TotalTokenCount = 1020,
            CachedInputTokenCount = 300,
            AdditionalCounts = new() { ["CacheCreationInputTokens"] = 600 }
        });
    }

    [Fact] void should_persist_the_priced_cost() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), _expected, Arg.Any<CancellationToken>());
    [Fact] void should_reconcile_the_prepaid_parent_at_the_priced_cost() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), new(_expected, 0), Arg.Any<CancellationToken>());
    [Fact] void should_not_log_a_rejection() => Rejections().ShouldBeEmpty();
}
