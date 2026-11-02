// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_resuming;

public class and_the_store_fails : given.an_answering_command
{
    void Establish()
    {
        Switch.BeginPause();
        Pauses.Set(false, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new InvalidOperationException("database down")));
    }

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Resume();
    }

    [Fact] void should_stay_paused() => Switch.Paused.ShouldBeTrue();
    [Fact] void should_say_it_was_not_saved() => Replied("answering stays paused").ShouldBeTrue();
}
