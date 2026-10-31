// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_haiku_is_approved_for_issue_drafting : Specification
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
        var configuration = new AnthropicOptions
        {
            IssueDraft = new() { Model = "claude-haiku-5-5", Effort = string.Empty },
            HaikuIssueDraftApproved = true
        };
        _client = new(_primary, null, configuration);
    }

    async Task Because() => await _client.GetResponseAsync(
        [new(ChatRole.User, "dummy conversation")],
        new() { AdditionalProperties = new() { [AnthropicAccounts.TaskKey] = "issue-draft" } });

    [Fact] void should_use_the_evaluated_model() => _sent.ModelId.ShouldEqual("claude-haiku-5-5");
    [Fact] void should_omit_unsupported_thinking() => _sent.Reasoning.ShouldBeNull();
    [Fact] void should_keep_a_bounded_output() => _sent.MaxOutputTokens.ShouldEqual(4096);
}
