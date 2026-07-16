// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Voyage AI API used for embeddings.
/// </summary>
public class VoyageOptions
{
    /// <summary>
    /// The embedding dimensionality the database schema is fixed to - the <c>embedding vector(1024)</c> column
    /// declared in <c>Storage/Migrations/v1_0_0.sql</c>. <see cref="Dimensions"/> is sent to Voyage as the
    /// requested <c>output_dimension</c>, so any value other than this makes every upsert fail at runtime with
    /// a vector-size mismatch; <see cref="DimensionsMatchSchema"/> guards it at startup.
    /// </summary>
    public const int SchemaDimensions = 1024;

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding model.
    /// </summary>
    public string Model { get; set; } = "voyage-4";

    /// <summary>
    /// Gets or sets the base URL of the Voyage AI API.
    /// </summary>
    public string Url { get; set; } = "https://api.voyageai.com/";

    /// <summary>
    /// Gets or sets the dimensionality of the embeddings. Must equal <see cref="SchemaDimensions"/> to match
    /// the fixed vector size in the database schema.
    /// </summary>
    public int Dimensions { get; set; } = SchemaDimensions;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dimensions"/> matches the vector size the database schema is
    /// fixed to (<see cref="SchemaDimensions"/>). Any mismatch fails every embedding upsert at runtime, so this
    /// is validated at startup to fail fast rather than mid-index.
    /// </summary>
    public bool DimensionsMatchSchema => Dimensions == SchemaDimensions;

    /// <summary>
    /// Gets or sets the maximum number of inputs sent in a single embeddings request. Voyage accepts up to
    /// 1,000 inputs and 320K tokens per request for <c>voyage-4</c>; the default of 128 stays well under both
    /// caps while keeping individual requests small and resilient (verified against the Voyage docs).
    /// </summary>
    public int BatchSize { get; set; } = 128;

    /// <summary>
    /// Gets or sets the number of times a transient embeddings failure (429 or 5xx) is retried before giving up.
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base delay, in milliseconds, for the exponential backoff between retries.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 500;
}
