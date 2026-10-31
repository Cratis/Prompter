// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class the_explicit_secondary_is_selected : given.durable_accounting
{
    IChatClient _secondary = null!;

    async Task Because()
    {
        _secondary = Substitute.For<IChatClient>();
        _secondary.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>()).Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer")));
        Options.Anthropic.Answer.PreferredAccount = "secondary";
        Options.Anthropic.Answer.AllowSecondary = true;
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        using var protectedSecondary = new BudgetedChat(_secondary, Microsoft.Extensions.Options.Options.Create(Options));
        using var router = new AnthropicAccounts(Chat, protectedSecondary, Options.Anthropic, Accounting, Options.Protection.Anthropic);
        await router.GetResponseAsync([new(ChatRole.User, "question")]);
    }

    [Fact] void should_not_probe_primary_first() => Provider.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_dispatch_secondary_once() => _secondary.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_fund_secondary_from_the_same_parent() => Store.Received(1).Reserve(Arg.Is<ProviderDispatch>(dispatch => dispatch.Account == "secondary"), Arg.Is<Guid?>(parent => parent.HasValue), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_fabricate_a_reset() => Options.Anthropic.PrimaryQuotaResetAt.ShouldBeNull();
}
