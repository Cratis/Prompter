// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Protection;

internal static partial class ProviderAccountingLogging
{
    [LoggerMessage(LogLevel.Warning, "Provider settlement rejected for {Provider} {Task} attempt {Attempt}: {Reason}; the reservation is retained")]
    internal static partial void SettlementRejected(this ILogger logger, string provider, string task, int attempt, SettlementRejection reason);

    [LoggerMessage(LogLevel.Information, "Provider attempt {Attempt} for {Provider} {Task} settled outside the prepaid envelope; the parent reservation is retained")]
    internal static partial void SettledOutsideEnvelope(this ILogger logger, int attempt, string provider, string task);
}
