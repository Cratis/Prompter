// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Represents an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> backed by the Voyage AI embeddings API.
/// Anthropic has no embeddings endpoint, and Voyage's free tier covers the whole Cratis corpus - see the
/// model decision record (D-5).
/// </summary>
/// <param name="httpClient">The client configured with the Voyage API as base address.</param>
/// <param name="prompterOptions">The Prompter options.</param>
public sealed class VoyageEmbeddings(HttpClient httpClient, IOptions<PrompterOptions> prompterOptions)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = new VoyageRequest([.. values], prompterOptions.Value.Voyage.Model, prompterOptions.Value.Voyage.Dimensions);
        var response = await httpClient.PostAsJsonAsync(new Uri("v1/embeddings", UriKind.Relative), request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VoyageResponse>(cancellationToken)
            ?? throw new EmbeddingGenerationFailed("response could not be deserialized");

        return new(result.Data
            .OrderBy(item => item.Index)
            .Select(item => new Embedding<float>(item.Embedding)));
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    sealed record VoyageRequest(
        [property: JsonPropertyName("input")] IEnumerable<string> Input,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("output_dimension")] int OutputDimension);

    sealed record VoyageResponse([property: JsonPropertyName("data")] IEnumerable<VoyageEmbedding> Data);

    sealed record VoyageEmbedding(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
