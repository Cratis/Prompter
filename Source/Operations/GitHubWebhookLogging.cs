// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Operations;

internal static partial class GitHubWebhookLogging
{
    [LoggerMessage(LogLevel.Information, "The documentation does not answer {Repository}#{Number}; staying silent")]
    internal static partial void IssueNotAnswerable(this ILogger<GitHubWebhook> logger, string repository, int number);

    [LoggerMessage(LogLevel.Error, "Answering an issue failed")]
    internal static partial void AnsweringIssueFailed(this ILogger<GitHubWebhook> logger);

    [LoggerMessage(LogLevel.Warning, "Could not announce an issue in the maintainer channel")]
    internal static partial void IssueNotificationFailed(this ILogger<GitHubWebhook> logger);

    [LoggerMessage(LogLevel.Warning, "Issue claim unavailable; skipping delivery")]
    internal static partial void IssueClaimFailed(this ILogger<GitHubWebhook> logger);

    [LoggerMessage(LogLevel.Information, "Ignoring previously claimed issue delivery")]
    internal static partial void IssueDeliveryRepeated(this ILogger<GitHubWebhook> logger);

    [LoggerMessage(LogLevel.Information, "Issue notification quota exhausted")]
    internal static partial void IssueNotificationLimited(this ILogger<GitHubWebhook> logger);
}
