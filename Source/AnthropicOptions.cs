// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Anthropic API used for answer generation.
/// </summary>
public class AnthropicOptions
{
    /// <summary>
    /// Gets or sets the API key. Falls back to the <c>ANTHROPIC_API_KEY</c> environment variable when empty.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model used for answer generation.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-5";
}
