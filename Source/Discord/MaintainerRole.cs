// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;

namespace Cratis.Prompter.Discord;

/// <summary>Detects maintainers from role data Discord signed into the payload. It never looks members up over REST.</summary>
public static class MaintainerRole
{
    /// <summary>Checks whether a user holds the configured maintainer role.</summary>
    /// <param name="options">The Discord options carrying <see cref="DiscordOptions.MaintainerRoleId"/>.</param>
    /// <param name="user">The user from the interaction or message payload.</param>
    /// <returns><see langword="true"/> only when the role is configured and listed on the user; otherwise <see langword="false"/>.</returns>
    public static bool Holds(DiscordOptions options, User? user) =>
        options.MaintainerRoleId is { } role && user is PartialGuildUser { RoleIds: var roles } && roles.Contains(role);
}
