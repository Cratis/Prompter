// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Builds identical task-aware provider clients for production and offline evaluation hosts.
/// </summary>
public static class AnthropicClients
{
    /// <summary>
    /// Creates one bounded account router. Secondary activation additionally requires accounting integration.
    /// </summary>
    /// <param name="options">Provider and task configuration.</param>
    /// <param name="accounting">The durable shared reservation integration.</param>
    /// <returns>A nonstreaming chat client.</returns>
    public static IChatClient Create(AnthropicOptions options, IProviderAccounting? accounting = null)
    {
        AnthropicConnection? primary = null;
        AnthropicConnection? secondary = null;
        try
        {
            primary = new(options.ApiKey);
            secondary = string.IsNullOrWhiteSpace(options.SecondaryApiKey) ? null : new AnthropicConnection(options.SecondaryApiKey);
            var router = new AnthropicAccounts(primary, secondary, options, accounting);
            primary = null;
            secondary = null;
            return router;
        }
        finally
        {
            primary?.Dispose();
            secondary?.Dispose();
        }
    }
}
