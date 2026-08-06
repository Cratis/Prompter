// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class FileIssueLogging
{
    [LoggerMessage(LogLevel.Information, "Issue filing refused: the user is over their rate limit")]
    internal static partial void IssueRateLimited(this ILogger<FileIssue> logger);

    [LoggerMessage(LogLevel.Information, "The model's reply could not be read as an issue draft")]
    internal static partial void CouldNotDraftIssue(this ILogger<FileIssue> logger);

    [LoggerMessage(LogLevel.Error, "Drafting an issue failed")]
    internal static partial void IssueDraftingFailed(this ILogger<FileIssue> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Could not deliver the issue-drafting apology")]
    internal static partial void IssueApologyFailed(this ILogger<FileIssue> logger, Exception exception);
}
