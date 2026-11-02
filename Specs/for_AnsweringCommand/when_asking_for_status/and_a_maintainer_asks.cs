// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_asking_for_status;

public class and_a_maintainer_asks : given.an_answering_command
{
    void Establish() => Switch.BeginPause();

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Status();
    }

    [Fact] void should_report_the_pause_and_the_configuration() => Replied("Answering is paused; configuration has answering enabled.").ShouldBeTrue();
}
