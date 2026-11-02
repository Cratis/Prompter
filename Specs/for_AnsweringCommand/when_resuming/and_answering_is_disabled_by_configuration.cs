// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_resuming;

public class and_answering_is_disabled_by_configuration : given.an_answering_command
{
    void Establish() => Options.Protection.AnsweringEnabled = false;

    async Task Because()
    {
        Invoke(Guild, Role);
        await Command.Resume();
    }

    [Fact] void should_resume_the_switch() => Switch.Paused.ShouldBeFalse();
    [Fact] void should_say_configuration_still_disables_answering() => Replied("still disabled by configuration").ShouldBeTrue();
}
