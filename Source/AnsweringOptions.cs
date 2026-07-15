// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options controlling retrieval and answer generation behavior.
/// </summary>
public class AnsweringOptions
{
    /// <summary>
    /// Gets or sets the maximum number of passages given to the model as grounding context.
    /// </summary>
    public int MaxPassages { get; set; } = 8;

    /// <summary>
    /// Gets or sets the minimum top-passage score below which Prompter refuses instead of answering.
    /// </summary>
    public double MinScore { get; set; } = 0.02;

    /// <summary>
    /// Gets or sets the maximum number of output tokens for a generated answer.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1024;
}
