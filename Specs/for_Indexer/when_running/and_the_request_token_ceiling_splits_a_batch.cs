// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_request_token_ceiling_splits_a_batch : given.an_indexer
{
    IndexRun _run = null!;
    long _firstBatchTokens;

    void Establish()
    {
        Options.Voyage.BatchSize = EmbeddingInputLimits.MaximumItems;
        Options.Voyage.IndexMaxRetries = 0;
        var pages = Enumerable.Range(0, 28).Select(index => new DocsPage(new PageUrl($"https://cratis.io/{index}.md"), $"# Title\n\n{new string('界', 3900)}")).ToArray();
        _firstBatchTokens = pages.Take(27).SelectMany(page => MarkdownChunker.Chunk(page.Url, page.Markdown)).Sum(chunk => IndexBudget.TokensFor(chunk.EmbeddingInput));
        Indexer = Create(new FakeDocsSite(pages));
    }

    async Task Because() => _run = await Indexer.Run();

    [Fact] void should_embed_all_inputs() => _run.Embedded.ShouldEqual(28);
    [Fact] void should_pack_up_to_the_shared_request_limit() => Embeddings.BatchSizes.ShouldEqual(27, 1);
    [Fact] void should_allow_more_than_the_old_request_ceiling() => (_firstBatchTokens > 300_000).ShouldBeTrue();
    [Fact] void should_include_utf8_and_framing_in_the_bound() => (_firstBatchTokens <= EmbeddingInputLimits.MaximumRequestTokens).ShouldBeTrue();
}
