// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord.Rest;

namespace Cratis.Prompter.Digest;

/// <summary>
/// Delivers maintainer posts with mentions suppressed.
/// </summary>
/// <param name="rest">The Discord REST client.</param>
public class DiscordMaintainerChannel(RestClient rest) : IMaintainerChannel
{
    /// <inheritdoc/>
    public async Task Post(ulong channelId, string content, CancellationToken cancellationToken) =>
        await rest.SendMessageAsync(channelId, new MessageProperties { Content = content, AllowedMentions = AllowedMentionsProperties.None }, cancellationToken: cancellationToken);
}
