// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_BudgetedChat.when_dispatching;

public class and_the_prompt_exceeds_its_reservation : for_PaidWork.given.a_paid_work
{
    IChatClient _provider = null!;
    Exception? _error;

    void Establish() => _provider = Substitute.For<IChatClient>();

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        using var chat = new BudgetedChat(_provider, Microsoft.Extensions.Options.Options.Create(Options));
        _error = await Catch.Exception(() => chat.GetResponseAsync([new(ChatRole.User, new string('a', 32001))]));
    }

    [Fact] void should_refuse_before_provider_dispatch() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_send_a_chat_request() => _provider.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
}
