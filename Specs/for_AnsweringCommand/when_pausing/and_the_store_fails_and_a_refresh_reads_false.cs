// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_pausing;

public class and_the_store_fails_and_a_refresh_reads_false : given.an_answering_command
{
    void Establish() => Pauses.Set(true, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new InvalidOperationException("database down")));

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Pause();
        Switch.Refresh(false, Switch.Version);
    }

    [Fact] void should_stay_paused() => Switch.Paused.ShouldBeTrue();
}
