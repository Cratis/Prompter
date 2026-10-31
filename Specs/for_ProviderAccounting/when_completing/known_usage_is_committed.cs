// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class known_usage_is_committed : given.durable_accounting
{
    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        await Send(Dispatch(), new() { InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 120 });
    }

    [Fact] void should_persist_the_validated_cost() => Store.Received(1).Complete(Arg.Any<ProviderDispatch>(), Options.Protection.AnthropicAnswer.Cost(100, 20), Arg.Any<CancellationToken>());
    [Fact] void should_refund_only_the_live_prepaid_parent() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), new(Options.Protection.AnthropicAnswer.Cost(100, 20), 0), Arg.Any<CancellationToken>());
    [Fact] void should_not_charge_the_shared_ledger_twice() => Ledger.Received(1).Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>());
}
