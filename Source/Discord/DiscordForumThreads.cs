// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Reads forum post metadata through the Discord REST client.
/// </summary>
/// <param name="rest">The REST client.</param>
public class DiscordForumThreads(RestClient rest) : IForumThreads
{
    readonly ConcurrentDictionary<ulong, ulong> _guilds = new();
    ulong _botId;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ForumThreadSnapshot>> Active(ulong forumId, CancellationToken cancellationToken)
    {
        if (!_guilds.TryGetValue(forumId, out var guildId))
        {
            var channel = await rest.GetChannelAsync(forumId, cancellationToken: cancellationToken);
            guildId = (channel as IGuildChannel)?.GuildId ?? 0;
            if (guildId == 0)
            {
                return [];
            }

            _guilds[forumId] = guildId;
        }

        var threads = await rest.GetActiveGuildThreadsAsync(guildId, cancellationToken: cancellationToken);
        return [.. threads
            .Where(thread => thread.ParentId == forumId)
            .Select(thread => new ForumThreadSnapshot(thread.Id, guildId, forumId, thread.OwnerId, thread.CreatedAt, thread.Metadata.Archived || thread.Metadata.Locked))];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ForumMessage>> Recent(ulong threadId, int limit, CancellationToken cancellationToken)
    {
        var messages = new List<ForumMessage>();
        var page = new PaginationProperties<ulong> { BatchSize = limit };
        await foreach (var message in rest.GetMessagesAsync(threadId, page).WithCancellation(cancellationToken))
        {
            messages.Add(new(message.Id, message.Author.Id, message.Author.IsBot, message.WebhookId is not null, message.Type is MessageType.Default or MessageType.Reply));
            if (messages.Count >= limit)
            {
                break;
            }
        }

        return messages;
    }

    /// <inheritdoc/>
    public async Task<ulong> BotId(CancellationToken cancellationToken)
    {
        if (_botId == 0)
        {
            _botId = (await rest.GetCurrentUserAsync(cancellationToken: cancellationToken)).Id;
        }

        return _botId;
    }
}
