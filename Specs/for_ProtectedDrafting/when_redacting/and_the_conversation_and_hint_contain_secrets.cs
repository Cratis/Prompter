// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedDrafting.when_redacting;

public class and_the_conversation_and_hint_contain_secrets : for_PaidWork.given.a_paid_work
{
    bool _safe;
    bool _marked;

    async Task Because()
    {
        var secret = new string(['s', 'k', '-', 'a', 'n', 't', '-']) + new string('a', 24);
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var text = string.Join('\n', call.ArgAt<IEnumerable<ChatMessage>>(0).Select(message => message.Text));
            _safe = !text.Contains(secret, StringComparison.Ordinal);
            _marked = text.Contains(SecretRedaction.Marker, StringComparison.Ordinal);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}")));
        });
        var drafting = new ProtectedDrafting(new IssueDrafting(chat), Work, Microsoft.Extensions.Options.Options.Create(Options));
        await drafting.Draft(secret, secret);
    }

    [Fact] void should_remove_values_from_both_inputs() => _safe.ShouldBeTrue();
    [Fact] void should_make_removal_visible() => _marked.ShouldBeTrue();
}
