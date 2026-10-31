// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class ReleaseNotesLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed reading release notes (HTTP status {StatusCode}); preserving the previous corpus")]
    internal static partial void FailedReadingReleases(this ILogger<ReleaseNotes> logger, int? statusCode);
}
