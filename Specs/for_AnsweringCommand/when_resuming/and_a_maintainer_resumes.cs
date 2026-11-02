// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_resuming;

public class and_a_maintainer_resumes : given.an_answering_command
{
    void Establish() => Switch.BeginPause();

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Resume();
    }

    [Fact] void should_resume_answering() => Switch.Paused.ShouldBeFalse();
    [Fact] void should_persist_the_flag() => Pauses.Received(1).Set(false, Arg.Any<CancellationToken>());
    [Fact] void should_confirm_privately() => Replied("Answering resumed.").ShouldBeTrue();
}
