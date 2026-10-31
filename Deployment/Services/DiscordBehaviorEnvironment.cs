// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Projects the optional, non-secret Discord behavior settings. Unset means the behavior stays off.</summary>
public static class DiscordBehaviorEnvironment
{
    /// <summary>Creates the runtime settings for the configured behaviors only.</summary>
    /// <param name="maintainerRoleId">The mentionable role for Ask a human, if any.</param>
    /// <param name="answerInThreadChannelIds">Comma-separated channels that get answers in threads, if any.</param>
    /// <returns>The environment values, empty when nothing is configured.</returns>
    /// <exception cref="InvalidDiscordId">A configured value is not a Discord id.</exception>
    public static IReadOnlyDictionary<string, string> Values(string? maintainerRoleId, string? answerInThreadChannelIds)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(maintainerRoleId))
        {
            values["Cratis__Prompter__Discord__MaintainerRoleId"] = Id(maintainerRoleId);
        }

        var channels = (answerInThreadChannelIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < channels.Length; index++)
        {
            values[$"Cratis__Prompter__Discord__AnswerInThreadChannelIds__{index}"] = Id(channels[index]);
        }

        return values;
    }

    static string Id(string value)
    {
        var trimmed = value.Trim();
        return ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? trimmed : throw new InvalidDiscordId();
    }
}
