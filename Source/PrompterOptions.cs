// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for Prompter, bound to the <c>Cratis:Prompter</c> configuration section.
/// </summary>
public class PrompterOptions
{
    /// <summary>
    /// Gets or sets the connection string for the Postgres database holding chunks and interactions.
    /// </summary>
    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=prompter;Username=prompter;Password=prompter";

    /// <summary>
    /// Gets or sets the base URL of the published documentation site to ingest.
    /// </summary>
    public string DocsSiteUrl { get; set; } = "https://cratis.io";

    /// <summary>
    /// Gets or sets the number of days interactions are retained before being purged.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the Discord specific options.
    /// </summary>
    public DiscordOptions Discord { get; set; } = new();

    /// <summary>
    /// Gets or sets the Anthropic specific options.
    /// </summary>
    public AnthropicOptions Anthropic { get; set; } = new();

    /// <summary>
    /// Gets or sets the Voyage AI specific options.
    /// </summary>
    public VoyageOptions Voyage { get; set; } = new();

    /// <summary>
    /// Gets or sets the answering specific options.
    /// </summary>
    public AnsweringOptions Answering { get; set; } = new();
}
