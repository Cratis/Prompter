// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_SavedPause.when_loading;

public class and_the_saved_flag_cannot_be_read : given.a_terminal_host
{
    string? _message;
    Exception? _after;

    void Establish() => Pauses.IsPaused(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<bool>(new InvalidOperationException("database down")));

    async Task Because()
    {
        _message = await SavedPause.Load(Closed, Pauses);
        _after = await TryBeginWork();
    }

    [Fact] void should_say_the_flag_could_not_be_read() => _message.ShouldContain("Could not read the saved answering pause flag");
    [Fact] void should_stay_closed() => _after.ShouldBeOfExactType<WorkRefused>();
}
