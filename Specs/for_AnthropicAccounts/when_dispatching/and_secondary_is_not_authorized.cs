// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_secondary_is_not_authorized : Specification
{
    [Fact]
    public async Task should_fail_closed_without_shared_accounting()
    {
        var primary = Substitute.For<IChatClient>();
        var secondary = Substitute.For<IChatClient>();
        var configuration = new AnthropicOptions { PrimaryQuotaResetAt = DateTimeOffset.UtcNow.AddHours(1), Answer = new() { AllowSecondary = true } };
        using var client = new AnthropicAccounts(primary, secondary, configuration);

        await Assert.ThrowsAsync<ProviderUnavailable>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], cancellationToken: TestContext.Current.CancellationToken));

        await primary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        await secondary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }
}
