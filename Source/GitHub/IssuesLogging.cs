// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.GitHub;

internal static partial class IssuesLogging
{
    [LoggerMessage(LogLevel.Information, "Filed issue #{Number} in {Repository}")]
    internal static partial void FiledIssue(this ILogger<Issues> logger, string repository, int number);

    [LoggerMessage(LogLevel.Information, "Commented on issue #{Number} in {Repository}")]
    internal static partial void CommentedOnIssue(this ILogger<Issues> logger, string repository, int number);

    [LoggerMessage(LogLevel.Debug, "Could not search {Repository} for similar issues; filing without a duplicate hint")]
    internal static partial void DuplicateSearchFailed(this ILogger<Issues> logger, Exception exception, string repository);
}
