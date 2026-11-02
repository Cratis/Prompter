// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Prompter.Discord;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_registering;

public class and_the_module_is_inspected : Specification
{
    SlashCommandAttribute _command = null!;
    string[] _subCommands = [];

    void Because()
    {
        _command = typeof(AnsweringCommand).GetCustomAttribute<SlashCommandAttribute>()!;
        _subCommands = [.. typeof(AnsweringCommand).GetMethods().Select(method => method.GetCustomAttribute<SubSlashCommandAttribute>()).OfType<SubSlashCommandAttribute>().Select(attribute => attribute.Name).Order()];
    }

    [Fact] void should_register_the_named_command() => _command.Name.ShouldEqual("answering");
    [Fact] void should_only_allow_guild_contexts() => _command.Contexts.ShouldEqual([InteractionContextType.Guild]);
    [Fact] void should_hide_the_command_from_members_without_manage_server() => _command.DefaultGuildPermissions.ShouldEqual(Permissions.ManageGuild);
    [Fact] void should_offer_pause_resume_and_status() => _subCommands.ShouldEqual(["pause", "resume", "status"]);
    [Fact] void should_use_the_existing_application_command_module() => typeof(AnsweringCommand).BaseType.ShouldEqual(typeof(ApplicationCommandModule<ApplicationCommandContext>));
}
