// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>Reads one bounded page from a public channel or public thread, never related content.</summary>
/// <param name="client">The Discord gateway and REST client.</param>
public sealed class DiscordConversationHistory(GatewayClient client) : IConversationHistory
{
    /// <inheritdoc/>
    public async Task<bool> IsPublic(ConversationLocation location, CancellationToken cancellationToken)
    {
        if (!client.Cache.Guilds.TryGetValue(location.GuildId, out var guild) || guild.EveryoneRole is not { } role) return false;
        var channel = await client.Rest.GetChannelAsync(
            location.ChannelId,
            new RestRequestProperties { RateLimitHandling = RestRateLimitHandling.NoRetry },
            cancellationToken);
        var scope = channel is GuildThread thread ? PublicThreadParent(thread, guild) : channel as IGuildChannel;
        if (scope is null || scope.GuildId != location.GuildId) return false;
        if (scope is not TextGuildChannel and not AnnouncementGuildChannel and not ForumGuildChannel) return false;
        return EveryoneCanView(scope, role.Permissions);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistoryMessage>> Before(ConversationLocation location, CancellationToken cancellationToken)
    {
        var messages = new List<HistoryMessage>();
        var pagination = new PaginationProperties<ulong> { From = location.MessageId, Direction = PaginationDirection.Before, BatchSize = 10 };
        await foreach (var message in client.Rest.GetMessagesAsync(
            location.ChannelId,
            pagination,
            new RestRequestProperties { RateLimitHandling = RestRateLimitHandling.NoRetry }).WithCancellation(cancellationToken))
        {
            messages.Add(new(
                message.Id,
                message.ChannelId,
                message.CreatedAt,
                message.Content,
                message.Author.IsBot,
                message.Author.Id == client.Id,
                message.Type is not MessageType.Default and not MessageType.Reply));
            if (messages.Count == 10) break;
        }
        return messages.AsReadOnly();
    }

    static IGuildChannel? PublicThreadParent(GuildThread thread, Guild guild)
    {
        if (thread.GuildId != guild.Id) return null;
        if (thread is not PublicGuildThread and not AnnouncementGuildThread and not ForumGuildThread) return null;
        return guild.Channels.GetValueOrDefault(thread.ParentId);
    }

    static bool EveryoneCanView(IGuildChannel scope, Permissions permissions)
    {
        if (scope.PermissionOverwrites.TryGetValue(scope.GuildId, out var everyone))
        {
            permissions = (permissions & ~everyone.Denied) | everyone.Allowed;
        }
        return permissions.HasFlag(Permissions.ViewChannel) || permissions.HasFlag(Permissions.Administrator);
    }
}
