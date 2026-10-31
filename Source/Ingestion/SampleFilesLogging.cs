// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class SampleFilesLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed reading samples (HTTP status {StatusCode}); preserving the previous corpus")]
    internal static partial void FailedReadingSamples(this ILogger<SampleFiles> logger, int? statusCode);

    [LoggerMessage(LogLevel.Warning, "Excluded sensitive sample '{Path}'")]
    internal static partial void SkippedSensitiveSample(this ILogger<SampleFiles> logger, string path);
}
