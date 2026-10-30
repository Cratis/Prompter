// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_haiku_has_not_passed_evaluation : Specification
{
    [Theory]
    [InlineData("answer", true)]
    [InlineData("issue-draft", false)]
    public async Task should_reject_the_override(string task, bool approved)
    {
        var primary = Substitute.For<IChatClient>();
        var configuration = new AnthropicOptions
        {
            Answer = new() { Model = "claude-haiku-5-5", Effort = string.Empty },
            IssueDraft = new() { Model = "claude-haiku-5-5", Effort = string.Empty },
            HaikuIssueDraftApproved = approved
        };
        using var client = new AnthropicAccounts(primary, null, configuration);
        var request = new ChatOptions { AdditionalProperties = new() { [AnthropicAccounts.TaskKey] = task } };

        await Assert.ThrowsAsync<ProviderUnavailable>(() => client.GetResponseAsync([new(ChatRole.User, "dummy")], request, TestContext.Current.CancellationToken));

        await primary.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }
}
