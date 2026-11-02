// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Projects the optional, non-secret Discord behavior settings. Unset means the behavior stays off.</summary>
public static class DiscordBehaviorEnvironment
{
    const string DefaultDelayMinutes = "30";
    const int MaximumDelayMinutes = 1440;

    /// <summary>Creates the runtime settings for the configured behaviors only.</summary>
    /// <param name="maintainerRoleId">The mentionable role for Ask a human, if any.</param>
    /// <param name="answerInThreadChannelIds">Comma-separated channels that get answers in threads, if any.</param>
    /// <param name="delayedForumAnswers">Comma-separated forum channels, each `id` or `id:minutes`, answered after a delay, if any.</param>
    /// <param name="deniedUserIds">Comma-separated users refused silently, if any.</param>
    /// <param name="allowedUserIds">Comma-separated users that are the only ones permitted, if any.</param>
    /// <returns>The environment values, empty when nothing is configured.</returns>
    /// <exception cref="InvalidDiscordId">A configured value is not a Discord id.</exception>
    /// <exception cref="InvalidForumDelay">A configured delay is not between 1 and 1440 minutes.</exception>
    public static IReadOnlyDictionary<string, string> Values(string? maintainerRoleId, string? answerInThreadChannelIds, string? delayedForumAnswers = null, string? deniedUserIds = null, string? allowedUserIds = null)
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

        var forums = (delayedForumAnswers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < forums.Length; index++)
        {
            var parts = forums[index].Split(':', 2, StringSplitOptions.TrimEntries);
            values[$"Cratis__Prompter__Discord__DelayedForums__{index}__ChannelId"] = Id(parts[0]);
            values[$"Cratis__Prompter__Discord__DelayedForums__{index}__DelayMinutes"] = parts.Length == 2 ? Minutes(parts[1]) : DefaultDelayMinutes;
        }

        AddList(values, "Cratis__Prompter__Protection__DeniedUsers", deniedUserIds);
        AddList(values, "Cratis__Prompter__Protection__AllowedUsers", allowedUserIds);
        return values;
    }

    static void AddList(Dictionary<string, string> values, string prefix, string? ids)
    {
        var entries = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < entries.Length; index++)
        {
            values[$"{prefix}__{index}"] = Id(entries[index]);
        }
    }

    static string Minutes(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) && minutes is >= 1 and <= MaximumDelayMinutes
            ? minutes.ToString(CultureInfo.InvariantCulture)
            : throw new InvalidForumDelay();

    static string Id(string value)
    {
        var trimmed = value.Trim();
        return ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? trimmed : throw new InvalidDiscordId();
    }
}
