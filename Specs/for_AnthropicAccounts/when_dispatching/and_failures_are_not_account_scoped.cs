// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_failures_are_not_account_scoped : Specification
{
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task should_not_switch_accounts(int status)
    {
        var primary = Substitute.For<IChatClient>();
        var secondary = Substitute.For<IChatClient>();
        var accounting = Substitute.For<IProviderAccounting>();
        accounting.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<CancellationToken>()).Returns(true);
        primary.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new HttpRequestException("dummy", null, (System.Net.HttpStatusCode)status));
        using var client = new AnthropicAccounts(primary, secondary, new() { Answer = new() { AllowSecondary = true } }, accounting);

        await Assert.ThrowsAsync<ProviderUnavailable>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], cancellationToken: TestContext.Current.CancellationToken));

        await secondary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        await accounting.Received(1).Complete(Arg.Is<ProviderDispatch>(dispatch => dispatch.Account == "primary"), null, "unknown");
    }
}
