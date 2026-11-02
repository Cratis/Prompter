// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Mirrors the persisted kill switch into memory: once at startup, then every 30 seconds, so a restart and a
/// manual database edit are both honored. A failed read keeps the last known value.
/// </summary>
/// <param name="answering">The in-memory switch.</param>
/// <param name="pauses">The persisted flag.</param>
/// <param name="time">The clock and timer.</param>
/// <param name="logger">The logger.</param>
public sealed class AnsweringSwitchRefresh(
    AnsweringSwitch answering,
    IAnsweringPauses pauses,
    TimeProvider time,
    ILogger<AnsweringSwitchRefresh> logger) : BackgroundService
{
    /// <summary>How often the persisted flag is re-read.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await Refresh(_startTimeout, cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await Refresh(_startTimeout, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    async Task Refresh(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeout);
            answering.Apply(await pauses.IsPaused(linked.Token));
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.RefreshFailed(exception);
        }
    }
}
