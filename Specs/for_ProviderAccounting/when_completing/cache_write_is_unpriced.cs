// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class cache_write_is_unpriced : given.durable_accounting
{
    async Task Because()
    {
        Options.Protection.AnthropicAnswer.CacheWriteUsdPerMillion = 0;
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        await Send(Dispatch(), new()
        {
            InputTokenCount = 1000,
            OutputTokenCount = 20,
            TotalTokenCount = 1020,
            AdditionalCounts = new() { ["CacheCreationInputTokens"] = 600 }
        });
    }

    [Fact] void should_retain_the_bound() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), null, Arg.Any<CancellationToken>());
    [Fact] void should_log_one_structured_reason() => Rejections().ShouldEqual([nameof(SettlementRejection.CacheWriteUnpriced)]);
}
