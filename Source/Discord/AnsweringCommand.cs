// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The maintainer-only <c language="csharp">/answering</c> kill switch. It is deliberately not routed through
/// <see cref="Admission"/>, so <c language="csharp">resume</c> keeps working while answering is paused.
/// </summary>
/// <param name="answering">The in-memory switch every entry point reads.</param>
/// <param name="pauses">The persisted flag.</param>
/// <param name="options">The Prompter options carrying the allowed guilds, maintainer role and static enable flag.</param>
/// <param name="logger">Logger for diagnostics.</param>
[SlashCommand("answering", "Pause or resume Prompter's answers", DefaultGuildPermissions = Permissions.ManageGuild, Contexts = [InteractionContextType.Guild])]
public class AnsweringCommand(
    AnsweringSwitch answering,
    IAnsweringPauses pauses,
    IOptions<PrompterOptions> options,
    ILogger<AnsweringCommand> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    static readonly TimeSpan _storeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Pauses answering. The in-memory switch is set first, so a failing write still pauses (fail safe).</summary>
    /// <returns>A task that completes when the reply is sent.</returns>
    [SubSlashCommand("pause", "Pause all answers until resumed")]
    public async Task Pause()
    {
        if (!Authorized())
        {
            await Reply("Only maintainers can do that.");
            return;
        }

        answering.Apply(true);
        if (await TryStore(true))
        {
            await Reply("Answering paused.");
        }
        else
        {
            await Reply("Paused until the next restart — could not save the setting.");
        }
    }

    /// <summary>Resumes answering. The switch is released only after the write succeeded.</summary>
    /// <returns>A task that completes when the reply is sent.</returns>
    [SubSlashCommand("resume", "Resume answering")]
    public async Task Resume()
    {
        if (!Authorized())
        {
            await Reply("Only maintainers can do that.");
            return;
        }

        if (!await TryStore(false))
        {
            await Reply("Could not resume — the setting was not saved; answering stays paused.");
            return;
        }

        answering.Apply(false);
        await Reply(options.Value.Protection.AnsweringEnabled
            ? "Answering resumed."
            : "Answering resumed, but answering is still disabled by configuration.");
    }

    /// <summary>Reports whether answering is paused and whether configuration enables it.</summary>
    /// <returns>A task that completes when the reply is sent.</returns>
    [SubSlashCommand("status", "Show whether answering is paused")]
    public async Task Status()
    {
        if (!Authorized())
        {
            await Reply("Only maintainers can do that.");
            return;
        }

        var configured = options.Value.Protection.AnsweringEnabled ? "enabled" : "disabled";
        await Reply($"Answering is {(answering.Paused ? "paused" : "running")}; configuration has answering {configured}.");
    }

    bool Authorized() =>
        Context.Interaction.GuildId is { } guild && options.Value.Protection.AllowedGuilds.Contains(guild) &&
        MaintainerRole.Holds(options.Value.Discord, Context.User);

    async Task<bool> TryStore(bool paused)
    {
        try
        {
            using var timeout = new CancellationTokenSource(_storeTimeout);
            await pauses.Set(paused, timeout.Token);
            return true;
        }
        catch (Exception exception)
        {
            logger.StoreFailed(exception);
            return false;
        }
    }

    async Task Reply(string text) => await Context.Interaction.SendResponseAsync(InteractionCallback.Message(SafeMessages.Ephemeral(text)));
}
