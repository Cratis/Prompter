// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_pricing_is_not_verified : given.a_paid_work
{
    Exception? _error;

    void Establish() => Options.Protection.AnthropicAnswer.Model = "another-model";

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await using var lease = await Work.BeginInteractive();
    });

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_touch_the_ledger() => Ledger.ReceivedCalls().ShouldBeEmpty();
}
