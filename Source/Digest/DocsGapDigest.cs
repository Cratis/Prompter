// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Digest;

/// <summary>
/// Posts weekly metadata counts; restarts compute the next future slot without a persisted marker.
/// </summary>
/// <param name="interactions">The metadata-only reader.</param>
/// <param name="channel">The message transport.</param>
/// <param name="options">The channel and schedule configuration.</param>
/// <param name="time">The UTC clock and timer.</param>
/// <param name="logger">The logger.</param>
public class DocsGapDigest(IDocsGapInteractions interactions, IMaintainerChannel channel, IOptions<PrompterOptions> options, TimeProvider time, ILogger<DocsGapDigest> logger) : BackgroundService
{
    bool _disabledLogged;

    /// <summary>
    /// Attempts one delivery for the seven days preceding the slot; failures do not terminate the loop.
    /// </summary>
    /// <param name="slot">The scheduled UTC delivery instant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Whether the digest was delivered.</returns>
    public async Task<bool> PostOnce(DateTimeOffset slot, CancellationToken ct)
    {
        if (options.Value.Discord.MaintainerChannelId is not { } channelId)
        {
            LogDisabled();
            return false;
        }

        try
        {
            var (from, to) = DigestSchedule.WindowFor(slot);
            var rows = await interactions.Read(from, to, ct);
            await channel.Post(channelId, DocsGapReport.Compose(rows, from, to), ct);
            logger.DigestPosted(rows.Count);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Do not log response bodies, exception text, row content or query parameters.
            logger.DigestFailed();
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.Discord.MaintainerChannelId is null)
        {
            LogDisabled();
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = time.GetUtcNow();
                var slot = DigestSchedule.NextSlot(now, options.Value.Discord.MaintainerDigestDay, options.Value.Discord.MaintainerDigestHourUtc);
                await Task.Delay(slot - now, time, stoppingToken);
                await PostOnce(slot, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown cancels the timer and any in-flight delivery.
        }
    }

    void LogDisabled()
    {
        if (!_disabledLogged)
        {
            logger.DigestDisabled();
            _disabledLogged = true;
        }
    }
}
