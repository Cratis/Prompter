// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.when_settling;

public class and_usage_is_unknown : given.a_paid_work
{
    SpendExposure _reserved = null!;
    SpendExposure _settled = null!;

    void Establish()
    {
        Ledger.Reserve(Arg.Do<SpendExposure>(value => _reserved = value), Arg.Any<CancellationToken>()).Returns(_ => (Guid?)Guid.NewGuid());
        Ledger.Reconcile(Arg.Any<Guid>(), Arg.Do<SpendExposure>(value => _settled = value), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.DispatchChat();
        lease.DispatchEmbedding(0.01m);

        // A timeout or disconnect after dispatch has no trustworthy chat usage to refund.
    }

    [Fact] void should_retain_the_full_ambiguous_chat_exposure() => _settled.Anthropic.ShouldEqual(_reserved.Anthropic);
    [Fact] void should_retain_each_dispatched_embedding_attempt() => _settled.Voyage.ShouldEqual(0.01m);
}
