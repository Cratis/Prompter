// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.given;

public class durable_accounting : for_PaidWork.given.a_paid_work
{
    protected IProviderAttemptStore Store { get; private set; } = null!;
    protected ProviderAccounting Accounting { get; private set; } = null!;
    protected IChatClient Provider { get; private set; } = null!;
    protected BudgetedChat Chat { get; private set; } = null!;

    protected ProviderDispatch Dispatch(int attempt = 1, Guid? operation = null) =>
        new(operation ?? Guid.NewGuid(), "anthropic", "primary", "answer", attempt, 100, Options.Answering.MaxOutputTokens, Options.Anthropic.Model, Options.Anthropic.Answer.Effort);

    protected async Task Send(ProviderDispatch dispatch, UsageDetails? usage = null)
    {
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, CancellationToken.None)).ShouldBeTrue();
        await Chat.GetResponseAsync([new(ChatRole.User, "question")]);
        await Accounting.Complete(dispatch, usage, "stop");
    }

    void Establish()
    {
        Store = Substitute.For<IProviderAttemptStore>();
        Store.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await Task.Yield();
            return true;
        });
        Store.Complete(Arg.Any<ProviderDispatch>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>()).Returns(true);
        Accounting = new(Store, Microsoft.Extensions.Options.Options.Create(Options));
        Provider = Substitute.For<IChatClient>();
        Provider.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>()).Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer")));
        Chat = new(Provider, Microsoft.Extensions.Options.Options.Create(Options));
    }

    void Destroy() => Chat.Dispose();
}
