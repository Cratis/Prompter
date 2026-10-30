// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_primary_allocation_is_confirmed_exhausted : Specification
{
    IChatClient _primary = null!;
    IChatClient _secondary = null!;
    IProviderAccounting _accounting = null!;
    ChatResponse _response = null!;
    ChatResponse _result = null!;
    AnthropicAccounts _client = null!;

    void Establish()
    {
        _primary = Substitute.For<IChatClient>();
        _secondary = Substitute.For<IChatClient>();
        _accounting = Substitute.For<IProviderAccounting>();
        _accounting.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<CancellationToken>()).Returns(true);
        _response = new(new ChatMessage(ChatRole.Assistant, "dummy answer")) { Usage = new() { InputTokenCount = 5, OutputTokenCount = 2 } };
        _secondary.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(_response);
        var configuration = new AnthropicOptions
        {
            PrimaryQuotaResetAt = DateTimeOffset.UtcNow.AddHours(1),
            Answer = new() { AllowSecondary = true }
        };
        _client = new(_primary, _secondary, configuration, _accounting);
    }

    async Task Because() => _result = await _client.GetResponseAsync([new(ChatRole.User, "dummy")]);

    [Fact] void should_use_secondary() => _result.ShouldEqual(_response);
    [Fact] void should_not_dispatch_to_exhausted_primary() => _primary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_reserve_secondary_independently() => _accounting.Received(1).Reserve(Arg.Is<ProviderDispatch>(dispatch => dispatch.Provider == "anthropic" && dispatch.Account == "secondary" && dispatch.Task == "answer" && dispatch.Attempt == 1), Arg.Any<CancellationToken>());
    [Fact] void should_reconcile_measured_usage() => _accounting.Received(1).Complete(Arg.Any<ProviderDispatch>(), _response.Usage, "unknown");
}
