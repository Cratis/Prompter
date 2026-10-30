// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Telemetry;

/// <summary>Durable aggregate pipeline outcomes shared by CLI, HTTP and scheduled indexing.</summary>
public interface IIndexHistory
{
    /// <summary>Records an actual pipeline invocation, not HTTP acceptance.</summary>
    /// <param name="cancellationToken">The invocation deadline.</param>
    /// <returns>Completion of the bounded write.</returns>
    Task Attempt(CancellationToken cancellationToken);

    /// <summary>Records completion independently of caller cancellation.</summary>
    /// <param name="outcome">A fixed success, failed or cancelled category.</param>
    /// <param name="run">Successful aggregate document and chunk counts.</param>
    /// <param name="seconds">Elapsed pipeline duration.</param>
    /// <returns>Completion of the bounded write.</returns>
    Task Complete(string outcome, IndexRun? run = null, double seconds = 0);
}
