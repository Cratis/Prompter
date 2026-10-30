// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_the_caller_cancels : Specification
{
    [Fact]
    public async Task should_not_reserve_or_dispatch()
    {
        var primary = Substitute.For<IChatClient>();
        var accounting = Substitute.For<IProviderAccounting>();
        using var client = new AnthropicAccounts(primary, null, new(), accounting);
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], cancellationToken: canceled.Token));

        await primary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        await accounting.DidNotReceive().Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<CancellationToken>());
    }
}
