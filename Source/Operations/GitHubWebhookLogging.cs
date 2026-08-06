// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Operations;

internal static partial class GitHubWebhookLogging
{
    [LoggerMessage(LogLevel.Information, "The documentation does not answer {Repository}#{Number}; staying silent")]
    internal static partial void IssueNotAnswerable(this ILogger<GitHubWebhook> logger, string repository, int number);

    [LoggerMessage(LogLevel.Error, "Answering {Repository}#{Number} failed")]
    internal static partial void AnsweringIssueFailed(this ILogger<GitHubWebhook> logger, Exception exception, string repository, int number);

    [LoggerMessage(LogLevel.Warning, "Could not announce {Repository}#{Number} in the maintainer channel")]
    internal static partial void IssueNotificationFailed(this ILogger<GitHubWebhook> logger, Exception exception, string repository, int number);
}
