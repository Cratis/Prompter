// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Digest;

/// <summary>
/// Posts to the configured maintainer channel without allowing mentions.
/// </summary>
public interface IMaintainerChannel
{
    /// <summary>
    /// Posts one message to the destination channel.
    /// </summary>
    /// <param name="channelId">The destination channel.</param>
    /// <param name="content">The message content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The delivery task.</returns>
    Task Post(ulong channelId, string content, CancellationToken cancellationToken);
}
