// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Bounded generation policy for one task, unchanged across accounts.
/// </summary>
public class TaskModelOptions
{
    /// <summary>
    /// Gets or sets an optional task-specific model override.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the maximum output, including thinking tokens.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the conservative prompt-token ceiling (UTF-8 bytes upper bound).
    /// </summary>
    public int MaxPromptTokens { get; set; } = 32_000;

    /// <summary>
    /// Gets or sets the whole operation deadline, capped at 60 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets adaptive thinking effort: low, medium or high. Empty omits thinking.
    /// </summary>
    public string Effort { get; set; } = "high";

    /// <summary>
    /// Gets or sets whether this task may use an authorized secondary account.
    /// </summary>
    public bool AllowSecondary { get; set; }

    /// <summary>Gets or sets the explicit account choice: primary or secondary. Unknown values deny dispatch.</summary>
    public string PreferredAccount { get; set; } = "primary";
}
