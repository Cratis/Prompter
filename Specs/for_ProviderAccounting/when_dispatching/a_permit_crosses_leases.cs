// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class a_permit_crosses_leases : given.durable_accounting
{
    Exception? _failure;

    async Task Because()
    {
        await using var original = await Work.BeginInteractive();
        original.Activate();
        var dispatch = Dispatch();
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, CancellationToken.None)).ShouldBeTrue();
        await using var unrelated = await Work.BeginInteractive();
        unrelated.Activate();
        _failure = await Catch.Exception(() => Chat.GetResponseAsync([new(ChatRole.User, "question")]));
    }

    [Fact] void should_refuse_the_wrong_request_context() => _failure.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_never_reach_transport() => Provider.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
}
