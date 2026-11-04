// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class cache_reads_without_a_read_price : given.durable_accounting
{
    async Task Because()
    {
        Options.Protection.AnthropicAnswer.CacheReadUsdPerMillion = 0;
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        await Send(Dispatch(), new() { InputTokenCount = 1000, OutputTokenCount = 20, TotalTokenCount = 1020, CachedInputTokenCount = 300 });
    }

    [Fact] void should_price_reads_at_the_input_price() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), Options.Protection.AnthropicAnswer.Cost(1000, 20), Arg.Any<CancellationToken>());
    [Fact] void should_not_log_a_rejection() => Rejections().ShouldBeEmpty();
}
