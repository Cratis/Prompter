// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.Fakes;

public sealed class TransientlyFailingEmbeddingGenerator(
    int failuresBeforeSuccess,
    HttpStatusCode status = HttpStatusCode.TooManyRequests) : IEmbeddingGenerator<string, Embedding<float>>
{
    public int Calls { get; private set; }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Calls <= failuresBeforeSuccess)
        {
            throw new HttpRequestException("simulated transient failure", null, status);
        }

        var inputs = values as IReadOnlyList<string> ?? [.. values];

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(inputs.Select(_ => new Embedding<float>(new float[4]))));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
