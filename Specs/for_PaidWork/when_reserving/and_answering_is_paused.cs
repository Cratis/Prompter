// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_answering_is_paused : given.a_paid_work
{
    Exception? _refused;
    Exception? _resumed;

    async Task Because()
    {
        Answering.BeginPause();
        _refused = await Catch.Exception(async () => await Work.BeginInteractive());
        Answering.Resumed();
        _resumed = await Catch.Exception(async () =>
        {
            await using var lease = await Work.BeginInteractive();
        });
    }

    [Fact] void should_refuse_paid_work() => _refused.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_allow_it_again_after_resuming() => _resumed.ShouldBeNull();
}
