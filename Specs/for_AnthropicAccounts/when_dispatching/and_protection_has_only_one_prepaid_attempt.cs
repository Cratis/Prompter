// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_protection_has_only_one_prepaid_attempt : for_PaidWork.given.a_paid_work
{
    IChatClient _provider = null!;
    Exception? _error;

    void Establish()
    {
        _provider = Substitute.For<IChatClient>();
        _provider.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new AnthropicCapacityFailure(TimeSpan.Zero));
    }

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        using var protectedClient = new BudgetedChat(_provider, Microsoft.Extensions.Options.Options.Create(Options));
        using var client = new AnthropicAccounts(protectedClient, null, Options.Anthropic);
        _error = await Catch.Exception(() => client.GetResponseAsync([new(ChatRole.User, "question")]));
    }

    [Fact] void should_refuse_the_unreserved_retry() => _error.ShouldBeOfExactType<ProviderUnavailable>();
    [Fact] void should_send_only_one_paid_request() => _provider.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_retain_unknown_chat_exposure() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), Arg.Is<SpendExposure>(exposure => exposure.Anthropic > 0), Arg.Any<CancellationToken>());
}
