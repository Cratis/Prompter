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
    /// Gets a value indicating whether the retention window is usable: it must be strictly positive, because
    /// a value of zero (or less) makes the purge's <c>occurred_at &lt; now() - make_interval(days =&gt; N)</c>
    /// predicate match every row, deleting the whole interactions table on the first sweep. Validated at
    /// startup so the misconfiguration fails fast rather than silently erasing history.
    /// </summary>
    public bool RetentionIsValid => RetentionDays > 0;

    /// <summary>
    /// Gets or sets the shared secret that authorizes <c>POST /reindex</c> calls. When empty, the endpoint
    /// refuses every request rather than allowing an unauthenticated re-index.
    /// </summary>
    public string ReindexSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ingestion specific options.
    /// </summary>
    public IngestionOptions Ingestion { get; set; } = new();

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
