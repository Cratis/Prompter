// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A disposable corpus where 25 fillers lean towards the query axis, so they fill the semantic arm and a
/// chunk can only be retrieved through the full-text arm or by expansion.
/// </summary>
public class a_corpus_with_a_semantic_crowd : a_disposable_documentation_corpus
{
    protected const int QueryAxis = 10;

    protected static float[] Query => Axes((QueryAxis, 1f));

    protected static float[] Orthogonal(int axis) => Axes((axis, 1f));

    protected static Chunk ChunkOf(string page, int index, string headingPath, string content) =>
        new(new($"{page}#{index}"), new(page), "Broad", headingPath, content, new($"hash-{page}-{index}"));

    protected void AskWith(float[] vector) => embeddings.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>())
        .Returns(_ => new GeneratedEmbeddings<Embedding<float>>([new(vector)]));

    protected async Task SeedCrowd()
    {
        for (var index = 0; index < 25; index++)
        {
            await Seed(
                new(new($"crowd_{index}"), new("https://cratis.io/crowd/"), "Crowd", "Other", "Unrelated crowd text.", new($"crowd-{index}")),
                Axes((QueryAxis, 0.5f), (100 + index, 1f)));
        }
    }
}
