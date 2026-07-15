// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options controlling per-user rate limiting of questions.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of questions a single user may ask within <see cref="WindowMinutes"/>.
    /// </summary>
    public int MaxQuestions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the length of the rolling rate-limit window, in minutes.
    /// </summary>
    public int WindowMinutes { get; set; } = 10;

    /// <summary>
    /// Gets a value indicating whether these options form a usable configuration: the window must be
    /// positive (a zero window divides by zero when computing the token refill rate, silently disabling
    /// limiting by producing an infinite allowance) and the allowance must be non-negative. Validated at
    /// startup so a misconfiguration fails fast rather than quietly letting everyone through.
    /// </summary>
    public bool IsValid => WindowMinutes > 0 && MaxQuestions >= 0;
}
