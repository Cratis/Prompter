// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Operations;

internal static partial class WebhookProcessingLogging
{
    [LoggerMessage(LogLevel.Information, "Webhook work canceled")]
    internal static partial void WebhookWorkCanceled(this ILogger<WebhookProcessing> logger);

    [LoggerMessage(LogLevel.Warning, "Webhook work failed")]
    internal static partial void WebhookWorkFailed(this ILogger<WebhookProcessing> logger);
}
