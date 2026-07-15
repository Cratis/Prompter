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
}
