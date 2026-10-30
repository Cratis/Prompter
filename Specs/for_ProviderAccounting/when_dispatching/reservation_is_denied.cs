// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class reservation_is_denied : given.durable_accounting
{
    Exception? _failure;

    async Task Because()
    {
        Store.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>()).Returns(false);
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        var dispatch = Dispatch();
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, CancellationToken.None)).ShouldBeFalse();
        _failure = await Catch.Exception(() => Chat.GetResponseAsync([new(ChatRole.User, "question")]));
    }

    [Fact] void should_not_fall_back_to_legacy_funding() => _failure.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_dispatch() => Provider.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
}
