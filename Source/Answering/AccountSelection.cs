// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Selects once per operation, never in response to transport errors.</summary>
public static class AccountSelection
{
    /// <summary>Selects an explicitly authorized account, preserving verified-reset compatibility.</summary>
    /// <param name="policy">The task opt-in and closed account preference.</param>
    /// <param name="primaryReset">An actual provider-confirmed reset, if supplied.</param>
    /// <param name="secondaryAvailable">Whether both a secondary client and durable accounting are available.</param>
    /// <param name="now">The selection clock.</param>
    /// <returns>The fixed logical account name.</returns>
    /// <exception cref="ProviderUnavailable">The configured account choice is not authorized or available.</exception>
    public static string Select(TaskModelOptions policy, DateTimeOffset? primaryReset, bool secondaryAvailable, DateTimeOffset now)
    {
        if (policy.PreferredAccount is not ("primary" or "secondary"))
        {
            throw new ProviderUnavailable("account_unavailable");
        }

        var secondary = policy.PreferredAccount == "secondary" || primaryReset > now;
        if (!secondary)
        {
            return "primary";
        }

        if (!policy.AllowSecondary || !secondaryAvailable)
        {
            throw new ProviderUnavailable("quota_exhausted");
        }

        return "secondary";
    }
}
