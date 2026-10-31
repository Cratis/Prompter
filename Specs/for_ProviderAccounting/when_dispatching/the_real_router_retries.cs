// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class the_real_router_retries : given.durable_accounting
{
    async Task Because()
    {
        Provider.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns<Task<ChatResponse>>(_ => throw new AnthropicCapacityFailure(TimeSpan.Zero), _ => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer"))));
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        using var router = new AnthropicAccounts(Chat, null, Options.Anthropic, Accounting, Options.Protection.Anthropic, Options.Protection.AnthropicAnswer);
        await router.GetResponseAsync([new(ChatRole.User, "question")]);
    }

    [Fact] void should_dispatch_twice_despite_one_prepaid_chat_slot() => Provider.Received(2).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_fund_the_second_attempt_independently() => Store.Received(1).Reserve(Arg.Is<ProviderDispatch>(dispatch => dispatch.Attempt == 2 && dispatch.Account == "primary"), null, Arg.Any<decimal>(), Arg.Any<CancellationToken>());
}
