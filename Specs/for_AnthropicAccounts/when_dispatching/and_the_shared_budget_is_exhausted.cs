// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_the_shared_budget_is_exhausted : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_dispatch_to_either_account(bool primaryExhausted)
    {
        var primary = Substitute.For<IChatClient>();
        var secondary = Substitute.For<IChatClient>();
        var accounting = Substitute.For<IProviderAccounting>();
        accounting.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<CancellationToken>()).Returns(false);
        var configuration = new AnthropicOptions
        {
            PrimaryQuotaResetAt = primaryExhausted ? DateTimeOffset.UtcNow.AddHours(1) : null,
            Answer = new() { AllowSecondary = true }
        };
        using var client = new AnthropicAccounts(primary, secondary, configuration, accounting);

        await Assert.ThrowsAsync<ProviderUnavailable>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], cancellationToken: TestContext.Current.CancellationToken));

        await primary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        await secondary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        await accounting.DidNotReceive().Complete(Arg.Any<ProviderDispatch>(), Arg.Any<UsageDetails>(), Arg.Any<string>());
    }
}
