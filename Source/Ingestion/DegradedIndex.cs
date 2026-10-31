// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Healthy sources were published, but failed sources retain their previous snapshots.</summary>
/// <param name="run">The published portion of the run.</param>
/// <param name="sources">The failed source URL prefixes.</param>
public class DegradedIndex(IndexRun run, IReadOnlyCollection<string> sources)
    : Exception($"Index partially published; retained failed sources: {string.Join(", ", sources)}")
{
    /// <summary>Gets the published portion of the run.</summary>
    public IndexRun Run { get; } = run;

    /// <summary>Gets the failed source URL prefixes.</summary>
    public IReadOnlyCollection<string> Sources { get; } = sources;
}
