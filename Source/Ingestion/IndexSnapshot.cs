// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Describes an atomic publication, including any retained source snapshots.</summary>
/// <param name="Changed">Staged chunks from healthy sources.</param>
/// <param name="Existing">All current and retained chunk identifiers.</param>
/// <param name="Pages">Fresh metadata for healthy sources only.</param>
public record IndexSnapshot(IReadOnlyCollection<EmbeddedChunk> Changed, IReadOnlySet<ChunkId> Existing, IReadOnlyCollection<DocumentationMetadata> Pages)
{
    /// <summary>Gets the URL prefixes whose old metadata and freshness must remain unchanged.</summary>
    public IReadOnlyCollection<string> PreservedSources { get; init; } = [];
}
