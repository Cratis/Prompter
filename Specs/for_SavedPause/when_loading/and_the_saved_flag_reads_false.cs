// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_SavedPause.when_loading;

public class and_the_saved_flag_reads_false : given.a_terminal_host
{
    Exception? _before;
    string? _message;
    Exception? _after;

    void Establish() => Pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(false);

    async Task Because()
    {
        _before = await TryBeginWork();
        _message = await SavedPause.Load(Closed, Pauses);
        _after = await TryBeginWork();
    }

    [Fact] void should_refuse_work_before_the_flag_is_read() => _before.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_report_nothing() => _message.ShouldBeNull();
    [Fact] void should_allow_work_afterwards() => _after.ShouldBeNull();
}
