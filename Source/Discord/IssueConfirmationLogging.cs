// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class IssueConfirmationLogging
{
    [LoggerMessage(LogLevel.Warning, "Unrecognized issue button custom id: {CustomId}")]
    internal static partial void UnrecognizedIssueCustomId(this ILogger<IssueConfirmation> logger, string customId);

    [LoggerMessage(LogLevel.Information, "Filed issue #{Number} in {Repository} from Discord")]
    internal static partial void FiledIssueFromDiscord(this ILogger<IssueConfirmation> logger, string repository, int number);

    [LoggerMessage(LogLevel.Error, "Filing an issue in {Repository} failed")]
    internal static partial void IssueFilingFailed(this ILogger<IssueConfirmation> logger, Exception exception, string repository);
}
