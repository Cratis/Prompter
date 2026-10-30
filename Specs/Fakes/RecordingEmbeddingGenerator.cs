// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.Fakes;

public sealed class RecordingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    readonly List<int> _batchSizes = [];
    readonly List<string?> _tasks = [];

    public IReadOnlyList<int> BatchSizes => _batchSizes;

    public Action<int>? BeforeGenerate { get; set; }

    public IReadOnlyList<string?> Tasks => _tasks;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inputs = values as IReadOnlyList<string> ?? [.. values];
        _batchSizes.Add(inputs.Count);
        _tasks.Add(options?.AdditionalProperties?.TryGetValue(Cratis.Prompter.Embeddings.EmbeddingRetry.TaskKey, out var task) == true ? task as string : null);
        BeforeGenerate?.Invoke(_batchSizes.Count);

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(inputs.Select(_ => new Embedding<float>(new float[4]))));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
