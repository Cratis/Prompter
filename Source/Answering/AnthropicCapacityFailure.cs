// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>
/// A sanitized, provider-wide retryable overload. It never authorizes switching accounts.
/// </summary>
/// <param name="retryAfter">The provider minimum delay, if supplied.</param>
public sealed class AnthropicCapacityFailure(TimeSpan? retryAfter) : Exception("Anthropic capacity unavailable.")
{
    /// <summary>
    /// Gets the minimum retry delay.
    /// </summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
