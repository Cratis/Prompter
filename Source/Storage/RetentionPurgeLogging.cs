// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Storage;

internal static partial class RetentionPurgeLogging
{
    [LoggerMessage(LogLevel.Information, "Retention purge removed {Purged} interaction(s) older than {RetentionDays} days")]
    internal static partial void RetentionPurged(this ILogger logger, int purged, int retentionDays);

    [LoggerMessage(LogLevel.Error, "Retention purge cycle failed; interactions were not pruned this run")]
    internal static partial void RetentionPurgeFailed(this ILogger logger, Exception exception);
}
