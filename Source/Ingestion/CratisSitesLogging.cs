// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class CratisSitesLogging
{
    [LoggerMessage(LogLevel.Information, "Skipping site page '{Url}' outside the configured crawl")]
    internal static partial void SitePageSkipped(this ILogger<CratisSites> logger, PageUrl url);

    [LoggerMessage(LogLevel.Warning, "Site fetch failed for '{Url}' ({FailureType})")]
    internal static partial void SiteFetchFailed(this ILogger<CratisSites> logger, PageUrl url, string failureType);
}
