// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_CorpusSources.when_enumerating;

public class and_sources_share_one_run : Specification
{
    [Theory]
    [InlineData(true, true, 3)]
    [InlineData(false, true, 2)]
    [InlineData(true, false, 2)]
    [InlineData(false, false, 1)]
    public async Task should_enumerate_the_enabled_union_in_order(bool releases, bool samples, int count)
    {
        using var responses = new Responses(false);
        using var client = new HttpClient(responses) { BaseAddress = new Uri("https://cratis.io/") };
        var options = Options.Create(new PrompterOptions());
        options.Value.Ingestion.ReleaseNotes.Repositories = ["Arc"];
        options.Value.Ingestion.ReleaseNotes.Enabled = releases;
        options.Value.Ingestion.Samples.Enabled = samples;
        var source = new CorpusSources(new(client, options, NullLogger<DocsSite>.Instance), new(client, options, NullLogger<ReleaseNotes>.Instance), new(client, options, NullLogger<SampleFiles>.Instance), options);
        var kinds = new List<SourceKind>();
        await foreach (var page in source.GetPages(TestContext.Current.CancellationToken)) kinds.Add(SourceKinds.Of(page.Url));
        kinds.Count.ShouldEqual(count);
        kinds[0].ShouldEqual(SourceKind.Documentation);
        kinds.SequenceEqual(kinds.Order()).ShouldBeTrue();
        responses.ReleaseRequests.ShouldEqual(releases ? 1 : 0);
        responses.SampleRequests.ShouldEqual(samples ? 1 : 0);
    }

    [Fact]
    public async Task should_propagate_a_sample_failure_after_other_sources()
    {
        using var responses = new Responses(true);
        using var client = new HttpClient(responses) { BaseAddress = new Uri("https://cratis.io/") };
        var options = Options.Create(new PrompterOptions());
        options.Value.Ingestion.ReleaseNotes.Repositories = ["Arc"];
        var source = new CorpusSources(new(client, options, NullLogger<DocsSite>.Instance), new(client, options, NullLogger<ReleaseNotes>.Instance), new(client, options, NullLogger<SampleFiles>.Instance), options);
        var count = 0;
        var error = await Catch.Exception(async () => { await foreach (var page in source.GetPages()) count++; });
        error.ShouldBeOfExactType<IncompleteCrawl>();
        count.ShouldEqual(2);
    }

    sealed class Responses(bool failSamples) : HttpMessageHandler
    {
        public int ReleaseRequests;
        public int SampleRequests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!;
            var body = "# Docs\n\nDocumentation.";
            var status = HttpStatusCode.OK;
            if (url.AbsolutePath == "/sitemap-0.xml") body = "<loc>https://cratis.io/arc</loc>";
            if (url.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal))
            {
                ReleaseRequests++;
                body = """[{"tag_name":"v1.2.3","body":"Changes","html_url":"https://github.com/Cratis/Arc/releases/tag/v1.2.3"}]""";
            }
            if (url.AbsolutePath.Contains("/git/trees/", StringComparison.Ordinal))
            {
                SampleRequests++;
                status = failSamples ? HttpStatusCode.Forbidden : HttpStatusCode.OK;
                body = """{"truncated":false,"tree":[{"path":"README.md","type":"blob","size":10}]}""";
            }
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
