// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class the_retry_has_no_funding : given.durable_accounting
{
    Exception? _failure;

    async Task Because()
    {
        Store.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<ProviderDispatch>().Attempt == 1);
        Provider.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>()).Returns<Task<ChatResponse>>(_ => throw new AnthropicCapacityFailure(TimeSpan.Zero));
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        using var router = new AnthropicAccounts(Chat, null, Options.Anthropic, Accounting, Options.Protection.Anthropic);
        _failure = await Catch.Exception(() => router.GetResponseAsync([new(ChatRole.User, "question")]));
    }

    [Fact] void should_refuse_the_unfunded_retry() => _failure.ShouldBeOfExactType<ProviderUnavailable>();
    [Fact] void should_dispatch_only_the_prepaid_attempt() => Provider.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
}
