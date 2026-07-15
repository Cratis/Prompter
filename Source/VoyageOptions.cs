// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Voyage AI API used for embeddings.
/// </summary>
public class VoyageOptions
{
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
    /// Gets or sets the dimensionality of the embeddings. Must match the vector size in the database schema.
    /// </summary>
    public int Dimensions { get; set; } = 1024;
}
