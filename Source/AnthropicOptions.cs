// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Anthropic API used for answer generation.
/// </summary>
public class AnthropicOptions
{
    /// <summary>
    /// Gets or sets the API key. Falls back to the <c language="csharp">ANTHROPIC_API_KEY</c> environment variable when empty.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model used for answer generation.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-5-5";

    /// <summary>
    /// Gets or sets the separately authorized secondary-account key. Empty disables secondary dispatch.
    /// </summary>
    public string SecondaryApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a provider-confirmed primary monthly allocation reset. No timestamp is inferred.
    /// </summary>
    public DateTimeOffset? PrimaryQuotaResetAt { get; set; }

    /// <summary>
    /// Gets or sets the answer task policy. Defaults to the short, fast Haiku 5.5 policy: no thinking effort and a
    /// 768-token output cap. Prices come from <see cref="Protection.ProtectionOptions.AnthropicAnswer"/>.
    /// </summary>
    public TaskModelOptions Answer { get; set; } = new() { Model = "claude-haiku-5-5", Effort = string.Empty, MaxOutputTokens = 768 };

    /// <summary>
    /// Gets or sets the issue drafting policy. Haiku requires explicit evaluation approval.
    /// </summary>
    public TaskModelOptions IssueDraft { get; set; } = new();

    /// <summary>
    /// Gets or sets whether measured issue-draft evaluation permits a Haiku override.
    /// </summary>
    public bool HaikuIssueDraftApproved { get; set; }
}
