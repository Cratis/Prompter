// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_an_enabled_repository_is_incomplete : given.an_indexer
{
    [Theory]
    [InlineData("failed")]
    [InlineData("empty")]
    [InlineData("skipped")]
    [InlineData("complete")]
    [InlineData("samples-empty")]
    [InlineData("samples-failed")]
    [InlineData("samples-timeout")]
    [InlineData("timeout")]
    [InlineData("failed-with-samples")]
    public async Task should_publish_healthy_sources_and_retain_failed_snapshots(string outcome)
    {
        Options.Ingestion.ReleaseNotes.Repositories = ["Arc", "Chronicle"];
        Options.Ingestion.Samples.Enabled = outcome.StartsWith("samples-", StringComparison.Ordinal) || outcome == "failed-with-samples";
        Options.Ingestion.Samples.Repository = "Cratis/Samples";
        var retained = new ChunkId(outcome.StartsWith("samples-", StringComparison.Ordinal)
            ? "https://github.com/Cratis/Samples/blob/main/Old.md#0"
            : "https://github.com/Cratis/Chronicle/releases/tag/v0.1#0");
        var stale = new ChunkId("https://cratis.io/deleted/#0");
        Chunks.GetHashes(Arg.Any<CancellationToken>()).Returns(new Dictionary<ChunkId, ContentHash> { [retained] = new("previous"), [stale] = new("stale") });
        using var responses = new Responses(outcome);
        using var client = new HttpClient(responses) { BaseAddress = new("https://cratis.io/") };
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        var source = new CorpusSources(new(client, config, NullLogger<DocsSite>.Instance), new(client, config, NullLogger<ReleaseNotes>.Instance), new(client, config, NullLogger<SampleFiles>.Instance), config);
        Indexer = Create(source);
        var error = await Catch.Exception(async () => await Indexer.Run(TestContext.Current.CancellationToken));
        if (outcome == "complete" || outcome == "empty" || outcome == "skipped")
        {
            // A valid response without publishable releases is an empty result for that repository, not a failure.
            var expected = outcome == "complete" ? 3 : 2;
            error.ShouldBeNull();
            await Lease.Received(1).CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Is<IReadOnlySet<ChunkId>>(ids => !ids.Contains(retained) && ids.Count == expected), Arg.Is<IReadOnlyCollection<DocumentationMetadata>>(pages => pages.Count == expected), Arg.Any<CancellationToken>());
        }
        else
        {
            error.ShouldBeOfExactType<DegradedIndex>();
            var degraded = (DegradedIndex)error!;
            var expectedPages = outcome.StartsWith("samples-", StringComparison.Ordinal) ? 3 : 1;
            if (outcome == "failed-with-samples") expectedPages = 2;
            await VerifyRetention(retained, stale, degraded, expectedPages);
        }
    }

    async Task VerifyRetention(ChunkId retained, ChunkId stale, DegradedIndex degraded, int expectedPages)
    {
        degraded.Sources.Any(prefix => retained.Value.StartsWith(prefix, StringComparison.Ordinal)).ShouldBeTrue();
        degraded.Run.Pages.ShouldEqual(expectedPages);
        await Lease.Received(1).CommitIndex(
            Arg.Is<IndexSnapshot>(snapshot =>
                snapshot.Existing.Contains(retained) && !snapshot.Existing.Contains(stale) &&
                snapshot.Pages.Count == expectedPages && snapshot.Changed.Count == expectedPages &&
                snapshot.PreservedSources.SequenceEqual(degraded.Sources)),
            Arg.Any<CancellationToken>());
        await Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
    }

    sealed class Responses(string outcome) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var status = HttpStatusCode.OK;
            var body = "# Docs\n\nDocumentation.";
            if (path == "/sitemap-0.xml") body = "<loc>https://cratis.io/arc</loc>";
            if (path.EndsWith("/git/trees/main", StringComparison.Ordinal)) return Task.FromResult(Samples());
            if (path.EndsWith("/releases", StringComparison.Ordinal))
            {
                var repository = path.Contains("/Chronicle/", StringComparison.Ordinal) ? "Chronicle" : "Arc";
                (status, body) = Release(repository);
            }
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }

        HttpResponseMessage Samples() => outcome switch
        {
            "samples-timeout" => throw new TaskCanceledException(),
            "samples-failed" => new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(string.Empty) },
            "samples-empty" => new(HttpStatusCode.OK) { Content = new StringContent("""{"truncated":false,"tree":[]}""") },
            _ => new(HttpStatusCode.OK) { Content = new StringContent("""{"truncated":false,"tree":[{"path":"README.md","type":"blob","size":10}]}""") }
        };

        (HttpStatusCode Status, string Body) Release(string repository)
        {
            if (repository == "Chronicle")
            {
                if (outcome == "failed" || outcome == "failed-with-samples") return (HttpStatusCode.ServiceUnavailable, string.Empty);
                if (outcome == "timeout") throw new TaskCanceledException();
                if (outcome == "empty") return (HttpStatusCode.OK, "[]");
                if (outcome == "skipped") return (HttpStatusCode.OK, """[{"tag_name":"v2","body":"Changes","draft":true}]""");
            }
            return (HttpStatusCode.OK, $$"""[{"tag_name":"v1.2.3","body":"Changes","html_url":"https://github.com/Cratis/{{repository}}/releases/tag/v1.2.3"}]""");
        }
    }
}
