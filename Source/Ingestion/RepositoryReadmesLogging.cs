// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class RepositoryReadmesLogging
{
    [LoggerMessage(LogLevel.Information, "Public README missing at '{Url}'")]
    internal static partial void ReadmeMissing(this ILogger<RepositoryReadmes> logger, PageUrl url);

    [LoggerMessage(LogLevel.Warning, "Public README fetch failed for '{Url}' (HTTP {StatusCode})")]
    internal static partial void ReadmeFetchFailed(this ILogger<RepositoryReadmes> logger, PageUrl url, int statusCode);
}
