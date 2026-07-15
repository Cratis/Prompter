// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Background service that enforces the GDPR retention window (D-8) by periodically purging interactions
/// older than <see cref="PrompterOptions.RetentionDays"/>. It runs a first sweep shortly after startup and
/// then once per day; a failing sweep is logged and swallowed so the loop survives to try again next cycle.
/// </summary>
/// <param name="log">The interaction log to purge.</param>
/// <param name="options">The Prompter options, read for the retention window announced in the logs.</param>
/// <param name="logger">The logger.</param>
public sealed class RetentionPurge(
    IInteractionLog log,
    IOptions<PrompterOptions> options,
    ILogger<RetentionPurge> logger) : BackgroundService
{
    /// <summary>
    /// The delay before the first purge, giving the host a moment to finish starting up while still pruning
    /// promptly on boot rather than waiting a whole cadence.
    /// </summary>
    public static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The cadence between purges. Retention is a coarse, day-granular window, so a daily sweep is ample.
    /// </summary>
    public static readonly TimeSpan Period = TimeSpan.FromDays(1);

    /// <summary>
    /// Runs a single purge cycle: deletes expired interactions and logs the outcome. Any failure other than
    /// cancellation is logged and swallowed so the surrounding timer loop never dies on a transient error.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Awaitable task.</returns>
    public async Task PurgeOnce(CancellationToken cancellationToken)
    {
        try
        {
            var purged = await log.PurgeExpired(cancellationToken);
            logger.RetentionPurged(purged, options.Value.RetentionDays);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.RetentionPurgeFailed(exception);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
            await PurgeOnce(stoppingToken);

            using var timer = new PeriodicTimer(Period);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PurgeOnce(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The host is shutting down - stop the loop cleanly.
        }
    }
}
