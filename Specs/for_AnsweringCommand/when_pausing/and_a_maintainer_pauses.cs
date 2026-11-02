// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_pausing;

public class and_a_maintainer_pauses : given.an_answering_command
{
    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Pause();
    }

    [Fact] void should_pause_answering() => Switch.Paused.ShouldBeTrue();
    [Fact] void should_persist_the_flag() => Pauses.Received(1).Set(true, Arg.Any<CancellationToken>());
    [Fact] void should_confirm_privately() => Replied("Answering paused.").ShouldBeTrue();
}
