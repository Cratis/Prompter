// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Hosting;

/// <summary>Validates explicit workload/account admission before a host can report healthy.</summary>
public static class AnsweringReadinessValidation
{
    /// <summary>The only guild approved for this bot.</summary>
    public const ulong ApprovedGuild = 1182595891576717413;

    /// <summary>The authoritative scalar list, independent of lower-priority indexed list entries.</summary>
    public const string GuildConfigurationKey = "Cratis:Prompter:Protection:ConfiguredAllowedGuilds";

    static readonly HashSet<string> _accounts = new(StringComparer.Ordinal) { "primary", "secondary" };

    /// <summary>Replaces indexed list binding and adds content-free startup validation.</summary>
    /// <param name="options">The bound options pipeline.</param>
    /// <param name="configuration">The layered application configuration.</param>
    /// <returns>The pipeline with authoritative guilds and readiness validation.</returns>
    public static OptionsBuilder<PrompterOptions> ValidateAnsweringReadiness(this OptionsBuilder<PrompterOptions> options, IConfiguration configuration) =>
        options.PostConfigure(value => value.Protection.AllowedGuilds = Guilds(configuration[GuildConfigurationKey]))
            .Validate(ValidGuilds, "Enabled answering requires an explicit nonempty approved guild allowlist; other guilds are not authorized.")
            .Validate(ValidAccounts, "Answering account preferences must be primary or secondary.")
            .Validate(SecondaryReady, "Enabled secondary answering requires task opt-in and a nonempty secondary key.");

    static ulong[] Guilds(string? configured) =>
        (configured ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var guild) ? guild : 0)
            .Distinct().ToArray();

    static bool ValidGuilds(PrompterOptions options)
    {
        var guilds = options.Protection.AllowedGuilds;
        var nonempty = !options.Protection.AnsweringEnabled || guilds.Count > 0;
        return nonempty && guilds.All(guild => guild == ApprovedGuild);
    }

    static bool ValidAccounts(PrompterOptions options) =>
        _accounts.Contains(options.Anthropic.Answer.PreferredAccount) && _accounts.Contains(options.Anthropic.IssueDraft.PreferredAccount);

    static bool SecondaryReady(PrompterOptions options)
    {
        if (!options.Protection.AnsweringEnabled)
        {
            return true;
        }

        var hasKey = !string.IsNullOrWhiteSpace(options.Anthropic.SecondaryApiKey);
        return TaskReady(options.Anthropic.Answer, hasKey) && TaskReady(options.Anthropic.IssueDraft, hasKey);
    }

    static bool TaskReady(TaskModelOptions task, bool hasKey) =>
        task.PreferredAccount != "secondary" || (task.AllowSecondary && hasKey);
}
