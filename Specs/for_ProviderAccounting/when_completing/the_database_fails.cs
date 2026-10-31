// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_completing;

public class the_database_fails : given.durable_accounting
{
    async Task Because()
    {
        Store.Complete(Arg.Any<ProviderDispatch>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>()).Returns<bool>(_ => throw new TimeoutException());
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        await Send(Dispatch(), new() { InputTokenCount = 1, OutputTokenCount = 1 });
    }

    [Fact] void should_retain_parent_funding() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), new(Options.Protection.Anthropic.Cost(Options.Protection.Anthropic.MaxInputTokens, Options.Answering.MaxOutputTokens), 0), Arg.Any<CancellationToken>());
}
