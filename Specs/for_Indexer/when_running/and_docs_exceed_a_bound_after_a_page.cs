// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_docs_exceed_a_bound_after_a_page : given.an_indexer
{
    readonly Responses _responses = new();
    HttpClient _client = null!;

    void Establish()
    {
        _client = new HttpClient(_responses, disposeHandler: false);
        var docs = new DocsSite(_client, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<DocsSite>.Instance);
        Indexer = Create(docs);
    }

    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    void Destroy()
    {
        _client.Dispose();
        _responses.Dispose();
    }

    [Fact] void should_fail_the_entire_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_embed_any_candidate() => Embeddings.BatchSizes.ShouldBeEmpty();
    [Fact] void should_not_publish_or_prune() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());

    sealed class Responses : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var text = request.RequestUri!.AbsolutePath switch
            {
                "/sitemap-0.xml" => "<loc>https://cratis.io/a</loc><loc>https://cratis.io/b</loc>",
                "/a.md" => "# A\n\nAlpha.",
                _ => new string('a', (2 * 1024 * 1024) + 1)
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) });
        }
    }
}
