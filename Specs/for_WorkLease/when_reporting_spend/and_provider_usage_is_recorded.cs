// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_WorkLease.when_reporting_spend;

public class and_provider_usage_is_recorded : for_PaidWork.given.a_paid_work
{
    decimal _known;
    decimal _unknown;

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.DispatchEmbedding(0.001m);
        lease.DispatchChat();
        _unknown = lease.Spent;
        lease.ChatUsed(0.002m);
        _known = lease.Spent;
    }

    [Fact] void should_sum_known_usage() => _known.ShouldEqual(0.003m);
    [Fact] void should_reserve_unknown_chat_exposure() => _unknown.ShouldEqual(ChatPricing.MaximumExposure(Options) + 0.001m);
}
