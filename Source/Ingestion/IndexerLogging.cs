// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class IndexerLogging
{
    [LoggerMessage(LogLevel.Information, "Index run completed - {Embedded} chunks embedded, {Unchanged} unchanged, {Removed} removed")]
    internal static partial void IndexRunCompleted(this ILogger<Indexer> logger, int embedded, int unchanged, int removed);
}
