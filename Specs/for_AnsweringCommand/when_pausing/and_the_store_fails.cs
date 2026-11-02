// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_pausing;

public class and_the_store_fails : given.an_answering_command
{
    void Establish() => Pauses.Set(true, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new InvalidOperationException("database down")));

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Pause();
    }

    [Fact] void should_still_pause_in_memory() => Switch.Paused.ShouldBeTrue();
    [Fact] void should_say_it_was_not_saved() => Replied("Paused until the next restart").ShouldBeTrue();
}
