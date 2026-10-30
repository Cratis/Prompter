// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class IndexerLogging
{
    [LoggerMessage(LogLevel.Information, "Index run completed in {Duration} - {Pages} pages, {Embedded} chunks embedded, {Unchanged} unchanged, {Removed} removed")]
    internal static partial void IndexRunCompleted(this ILogger<Indexer> logger, int pages, int embedded, int unchanged, int removed, TimeSpan duration);

    [LoggerMessage(LogLevel.Information, "Index exposure reserved: {Tokens} retry-inclusive tokens, {Dollars} dollars (upper bounds, not measured usage)")]
    internal static partial void IndexExposureReserved(this ILogger<Indexer> logger, long tokens, decimal dollars);

    [LoggerMessage(LogLevel.Information, "Index batch: {Attempted} inputs attempted, {Succeeded} inputs previously staged")]
    internal static partial void IndexBatchStarted(this ILogger<Indexer> logger, int attempted, int succeeded);

    [LoggerMessage(LogLevel.Warning, "Index batch failed: {Failed} inputs in failed batch, {Staged} inputs staged; entire run discarded, provider charge may be unknown")]
    internal static partial void IndexBatchFailed(this ILogger<Indexer> logger, int failed, int staged);

    [LoggerMessage(LogLevel.Warning, "Crawl discovered no chunks - leaving the existing corpus intact instead of removing it. The documentation source is likely unreachable or its format changed.")]
    internal static partial void SkippedRemovalForEmptyCrawl(this ILogger<Indexer> logger);
}
