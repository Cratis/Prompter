// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.given;

public class durable_accounting : for_PaidWork.given.a_paid_work
{
    protected IProviderAttemptStore Store { get; private set; } = null!;
    protected ILogger<ProviderAccounting> Logger { get; } = Substitute.For<ILogger<ProviderAccounting>>();
    protected ProviderAccounting Accounting { get; private set; } = null!;
    protected IChatClient Provider { get; private set; } = null!;
    protected BudgetedChat Chat { get; private set; } = null!;

    protected ProviderDispatch Dispatch(int attempt = 1, Guid? operation = null) =>
        new(operation ?? Guid.NewGuid(), "anthropic", "primary", "answer", attempt, 100, Options.Answering.MaxOutputTokens, Options.Anthropic.Answer.Model!, Options.Anthropic.Answer.Effort);

    protected async Task Send(ProviderDispatch dispatch, UsageDetails? usage = null)
    {
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, CancellationToken.None)).ShouldBeTrue();
        await Chat.GetResponseAsync([new(ChatRole.User, "question")]);
        await Accounting.Complete(dispatch, usage, "stop");
    }

    protected string[] Rejections() =>
        [.. Logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log" && Equals(call.GetArguments()[0], LogLevel.Warning))
            .Select(call => (IEnumerable<KeyValuePair<string, object?>>)call.GetArguments()[2]!)
            .Select(values => values.Single(pair => pair.Key == "Reason").Value!.ToString()!)];

    void Establish()
    {
        Options.Anthropic.Answer.MaxOutputTokens = Options.Answering.MaxOutputTokens;
        Options.Anthropic.IssueDraft.MaxOutputTokens = Options.Answering.MaxOutputTokens;
        Store = Substitute.For<IProviderAttemptStore>();
        Store.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await Task.Yield();
            return true;
        });
        Store.Complete(Arg.Any<ProviderDispatch>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>()).Returns(true);
        Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        Accounting = new(Store, Microsoft.Extensions.Options.Options.Create(Options), Logger);
        Provider = Substitute.For<IChatClient>();
        Provider.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>()).Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer")));
        Chat = new(Provider, Microsoft.Extensions.Options.Options.Create(Options));
    }

    void Destroy() => Chat.Dispose();
}
