// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents the outcome of a single index run, returned by <see cref="IIndexer.Run"/> so callers - the CLI
/// today, the re-index webhook and scheduled runs later - can report and act on the same summary.
/// </summary>
/// <param name="Pages">The number of documentation pages fetched and chunked.</param>
/// <param name="Embedded">The number of chunks whose content changed and were re-embedded.</param>
/// <param name="Unchanged">The number of chunks whose content was unchanged and skipped.</param>
/// <param name="Removed">The number of chunks removed because their source no longer exists.</param>
/// <param name="Duration">The wall-clock duration of the run.</param>
public record IndexRun(int Pages, int Embedded, int Unchanged, int Removed, TimeSpan Duration);
