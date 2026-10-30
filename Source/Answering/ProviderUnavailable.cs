// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>
/// A sanitized generation failure, safe to log without provider bodies or request text.
/// </summary>
/// <param name="reason">A fixed failure category.</param>
public sealed class ProviderUnavailable(string reason) : Exception($"Generation unavailable: {reason}.")
{
    /// <summary>Gets the HTTP-derived category, never provider error text.</summary>
    public string Category { get; init; } = "provider_failure";
}
