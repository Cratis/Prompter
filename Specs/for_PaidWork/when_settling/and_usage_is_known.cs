// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.when_settling;

public class and_usage_is_known : given.a_paid_work
{
    SpendExposure _settled = null!;
    bool _released;

    void Establish() => Ledger.Reconcile(Arg.Any<Guid>(), Arg.Do<SpendExposure>(value => _settled ??= value), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

    async Task Because()
    {
        await using (var lease = await Work.BeginInteractive())
        {
            lease.DispatchChat();
            lease.ChatUsed(0.002m);
        }

        await using var first = await Work.BeginInteractive();
        await using var second = await Work.BeginInteractive();
        await using var third = await Work.BeginInteractive();
        _released = true;
    }

    [Fact] void should_reconcile_known_chat_usage() => _settled.Anthropic.ShouldEqual(0.002m);
    [Fact] void should_release_the_slot() => _released.ShouldBeTrue();
}
