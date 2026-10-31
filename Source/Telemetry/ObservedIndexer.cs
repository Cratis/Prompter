// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Telemetry;

/// <summary>Observes the pipeline itself, including detached HTTP work and process-local CLI runs.</summary>
/// <param name="inner">The protected pipeline.</param>
/// <param name="history">The durable aggregate sink.</param>
public sealed class ObservedIndexer(IIndexer inner, IIndexHistory history) : IIndexer
{
    /// <inheritdoc/>
    public Task<IndexRun> Run(CancellationToken cancellationToken = default) => Run(new IndexRequest(), cancellationToken);

    /// <inheritdoc/>
    public async Task<IndexRun> Run(IndexRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        await history.Attempt(cancellationToken);
        IndexRun? run = null;
        var outcome = "failed";
        try
        {
            run = await inner.Run(request, cancellationToken);
            outcome = "success";
            return run;
        }
        catch (OperationCanceledException)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            await history.Complete(outcome, run, Stopwatch.GetElapsedTime(start).TotalSeconds);
        }
    }
}
