// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Prompter.Discord;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Specs.for_AskPrompter.when_registering;

public class and_the_module_is_inspected : Specification
{
    MessageCommandAttribute _command = null!;
    void Because() => _command = typeof(AskPrompter).GetMethod(nameof(AskPrompter.Handle))!.GetCustomAttribute<MessageCommandAttribute>()!;
    [Fact] void should_register_the_named_message_command() => _command.Name.ShouldEqual("Ask Prompter");
    [Fact] void should_only_allow_guild_contexts() => _command.Contexts.ShouldEqual([InteractionContextType.Guild]);
    [Fact] void should_use_the_existing_application_command_module() => typeof(AskPrompter).BaseType.ShouldEqual(typeof(ApplicationCommandModule<ApplicationCommandContext>));
}
