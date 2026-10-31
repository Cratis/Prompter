// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Digest;

internal static partial class DocsGapDigestLogging
{
    [LoggerMessage(LogLevel.Information, "Weekly docs-gap digest disabled; no maintainer channel configured")]
    internal static partial void DigestDisabled(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Weekly docs-gap digest posted from {Count} metadata rows")]
    internal static partial void DigestPosted(this ILogger logger, int count);

    [LoggerMessage(LogLevel.Warning, "Weekly docs-gap digest failed; retrying at the next weekly slot")]
    internal static partial void DigestFailed(this ILogger logger);
}
