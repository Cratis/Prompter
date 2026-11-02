// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class AnsweringCommandLogging
{
    [LoggerMessage(LogLevel.Error, "Could not store the answering pause flag")]
    internal static partial void StoreFailed(this ILogger<AnsweringCommand> logger, Exception exception);
}
