// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class two_funded_attempts : given.durable_accounting
{
    ProviderDispatch _first = null!;
    ProviderDispatch _second = null!;

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        _first = Dispatch();
        _second = Dispatch(2, _first.OperationId);
        await Send(_first);
        await Send(_second, new() { InputTokenCount = 100, OutputTokenCount = 20 });
    }

    [Fact] void should_dispatch_both_attempts_in_one_lease() => Provider.Received(2).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_claim_first_attempt_from_prepaid_parent() => Store.Received(1).Reserve(_first, Arg.Is<Guid?>(id => id.HasValue), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    [Fact] void should_fund_retry_independently() => Store.Received(1).Reserve(_second, null, Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_refund_unknown_first_attempt_from_known_retry() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), new(Options.Protection.Anthropic.Cost(Options.Protection.Anthropic.MaxInputTokens, Options.Answering.MaxOutputTokens), 0), Arg.Any<CancellationToken>());
}
