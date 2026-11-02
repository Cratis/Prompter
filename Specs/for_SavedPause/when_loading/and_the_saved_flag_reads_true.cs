// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_SavedPause.when_loading;

public class and_the_saved_flag_reads_true : given.a_terminal_host
{
    string? _message;
    Exception? _after;

    void Establish() => Pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(true);

    async Task Because()
    {
        _message = await SavedPause.Load(Closed, Pauses);
        _after = await TryBeginWork();
    }

    [Fact] void should_say_a_maintainer_paused_answering() => _message.ShouldContain("paused by a maintainer");
    [Fact] void should_keep_refusing_work() => _after.ShouldBeOfExactType<WorkRefused>();
}
