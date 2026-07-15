// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class DocsSiteLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed fetching page '{Url}' - skipping it for this run")]
    internal static partial void FailedFetchingPage(this ILogger<DocsSite> logger, PageUrl url, Exception exception);
}
