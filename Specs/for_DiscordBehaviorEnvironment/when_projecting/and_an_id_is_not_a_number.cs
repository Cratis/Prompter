// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_an_id_is_not_a_number : Specification
{
    Exception? _roleError;
    Exception? _channelError;

    void Because()
    {
        _roleError = Catch.Exception(() => DiscordBehaviorEnvironment.Values("role", null));
        _channelError = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, "10,abc"));
    }

    [Fact] void should_reject_the_role() => _roleError.ShouldBeOfExactType<InvalidDiscordId>();
    [Fact] void should_reject_a_channel() => _channelError.ShouldBeOfExactType<InvalidDiscordId>();
}
