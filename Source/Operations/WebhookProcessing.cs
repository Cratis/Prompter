// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Operations;

/// <summary>Owns nonqueued webhook capacity before any payload closure starts executing.</summary>
/// <param name="options">Webhook throughput and concurrency limits.</param>
/// <param name="clock">The shared deadline clock.</param>
/// <param name="logger">Content-free lifecycle diagnostics.</param>
public sealed class WebhookProcessing(IOptions<PrompterOptions> options, TimeProvider clock, ILogger<WebhookProcessing> logger)
{
    readonly BoundedWork _work = new(options.Value.Protection.WebhookConcurrency, options.Value.Protection.WebhookPerMinute, clock);

    /// <summary>Synchronously reserves capacity before starting an observed, deadline-bound callback.</summary>
    /// <param name="work">The callback retaining one authenticated issue.</param>
    /// <param name="applicationStopping">Application shutdown cancellation.</param>
    /// <returns>Whether work was accepted; rejected work is not started or retained.</returns>
    public bool TryStart(Func<CancellationToken, Task> work, CancellationToken applicationStopping)
    {
        if (applicationStopping.IsCancellationRequested) return false;
        WorkTurn? turn = null;
        try
        {
            turn = _work.TryEnter();
            if (turn is null) return false;
            _ = Run(work, turn, applicationStopping);
            turn = null; // Ownership moves to the observed lifecycle.
            return true;
        }
        finally
        {
            turn?.Dispose();
        }
    }

    async Task Run(Func<CancellationToken, Task> work, WorkTurn turn, CancellationToken applicationStopping)
    {
        using (turn)
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60), clock))
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, applicationStopping))
        {
            try
            {
                deadline.Token.ThrowIfCancellationRequested();
                await work(deadline.Token);
            }
            catch (OperationCanceledException)
            {
                logger.WebhookWorkCanceled();
            }
            catch (Exception)
            {
                logger.WebhookWorkFailed();
            }
        }
    }
}
