// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

/// <summary>Sonnet 5.5 rejects disabled thinking with an HTTP 400, so an empty effort omits thinking there.</summary>
public class and_a_model_that_always_thinks_has_no_effort : Specification
{
    IChatClient _primary = null!;
    AnthropicAccounts _client = null!;
    ChatOptions _sent = null!;

    void Establish()
    {
        _primary = Substitute.For<IChatClient>();
        _primary.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _sent = call.Arg<ChatOptions>();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "dummy draft"));
            });
        _client = new(_primary, null, new AnthropicOptions { IssueDraft = new() { Model = "claude-sonnet-5-5", Effort = string.Empty } });
    }

    async Task Because() => await _client.GetResponseAsync(
        [new(ChatRole.User, "dummy conversation")],
        new() { AdditionalProperties = new() { [AnthropicAccounts.TaskKey] = "issue-draft" } });

    [Fact] void should_use_the_configured_model() => _sent.ModelId.ShouldEqual("claude-sonnet-5-5");
    [Fact] void should_omit_thinking() => _sent.Reasoning.ShouldBeNull();
}
