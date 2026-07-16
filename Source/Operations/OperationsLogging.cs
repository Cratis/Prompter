// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Operations;

internal static partial class OperationsLogging
{
    [LoggerMessage(LogLevel.Information, "Reindex requested via webhook; starting background index run")]
    internal static partial void ReindexStarted(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Reindex completed in {Duration} - {Pages} pages, {Embedded} embedded, {Unchanged} unchanged, {Removed} removed")]
    internal static partial void ReindexCompleted(this ILogger logger, TimeSpan duration, int pages, int embedded, int unchanged, int removed);

    [LoggerMessage(LogLevel.Error, "Background reindex run failed")]
    internal static partial void ReindexFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Background reindex run cancelled by host shutdown")]
    internal static partial void ReindexCancelled(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Reindex rejected: a run is already in progress")]
    internal static partial void ReindexAlreadyRunning(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Reindex rejected: missing or invalid secret")]
    internal static partial void ReindexUnauthorized(this ILogger logger);
}
