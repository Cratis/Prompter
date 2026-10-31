// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class a_permit_is_reused : given.durable_accounting
{
    Exception? _failure;
    bool _duplicate;

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        var dispatch = Dispatch();
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, CancellationToken.None)).ShouldBeTrue();
        await Chat.GetResponseAsync([new(ChatRole.User, "question")]);
        _duplicate = await Accounting.Reserve(dispatch, CancellationToken.None);
        _failure = await Catch.Exception(() => Chat.GetResponseAsync([new(ChatRole.User, "again")]));
    }

    [Fact] void should_not_regrant_the_scope() => _duplicate.ShouldBeFalse();
    [Fact] void should_deny_reuse() => _failure.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_only_dispatch_once() => Provider.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
}
