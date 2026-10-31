// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_the_provider_is_overloaded : Specification
{
    [Theory]
    [InlineData(503)]
    [InlineData(529)]
    public async Task should_bound_attempts_and_never_switch_accounts(int status)
    {
        using var http = new ProviderHttp((HttpStatusCode)status, "0");
        using var primary = new AnthropicConnection("dummy-api-key", http);
        var secondary = Substitute.For<IChatClient>();
        using var client = new AnthropicAccounts(primary, secondary, new() { Answer = new() { AllowSecondary = true } });

        await Assert.ThrowsAsync<ProviderUnavailable>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], cancellationToken: TestContext.Current.CancellationToken));

        http.Calls.ShouldEqual(2);
        await secondary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }
}
