// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class IndexerLogging
{
    [LoggerMessage(LogLevel.Information, "Index run completed in {Duration} - {Pages} pages, {Embedded} chunks embedded, {Unchanged} unchanged, {Removed} removed")]
    internal static partial void IndexRunCompleted(this ILogger<Indexer> logger, int pages, int embedded, int unchanged, int removed, TimeSpan duration);

    [LoggerMessage(LogLevel.Warning, "Crawl discovered no chunks - leaving the existing corpus intact instead of removing it. The documentation source is likely unreachable or its format changed.")]
    internal static partial void SkippedRemovalForEmptyCrawl(this ILogger<Indexer> logger);
}
