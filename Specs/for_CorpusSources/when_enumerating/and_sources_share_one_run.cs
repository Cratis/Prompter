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
        DisableNewSources(options.Value);
        var source = Create(client, options);
        var kinds = new List<SourceKind>();
        await foreach (var page in source.GetPages(TestContext.Current.CancellationToken)) kinds.Add(SourceKinds.Of(page.Url));
        kinds.Count.ShouldEqual(count);
        kinds[0].ShouldEqual(SourceKind.Documentation);
        kinds.SequenceEqual(kinds.Order()).ShouldBeTrue();
        responses.ReleaseRequests.ShouldEqual(releases ? 1 : 0);
        responses.SampleRequests.ShouldEqual(samples ? 1 : 0);
    }

    [Fact]
    public async Task should_isolate_a_sample_failure_after_other_sources()
    {
        using var responses = new Responses(true);
        using var client = new HttpClient(responses) { BaseAddress = new Uri("https://cratis.io/") };
        var options = Options.Create(new PrompterOptions());
        options.Value.Ingestion.ReleaseNotes.Repositories = ["Arc"];
        DisableNewSources(options.Value);
        var source = Create(client, options);
        var count = 0;
        var error = await Catch.Exception(async () => { await foreach (var page in source.GetPages()) count++; });
        error.ShouldBeNull();
        count.ShouldEqual(2);
        source.FailedSources.Single().ShouldEqual($"https://github.com/{options.Value.Ingestion.Samples.Repository}/blob/");
    }

    [Fact]
    public async Task should_isolate_one_site_and_continue_healthy_sources()
    {
        using var responses = new Responses(false);
        using var client = new HttpClient(responses) { BaseAddress = new Uri("https://cratis.io/") };
        var options = Options.Create(new PrompterOptions());
        DisableNewSources(options.Value);
        options.Value.Ingestion.Sites.Company.Enabled = true;
        options.Value.Ingestion.Sites.Direct.Enabled = true;
        options.Value.Ingestion.ReleaseNotes.Repositories = ["Arc"];
        var source = Create(client, options);
        var pages = new List<DocsPage>();
        await foreach (var page in source.GetPages(TestContext.Current.CancellationToken)) pages.Add(page);
        source.FailedSources.ShouldContainOnly("https://cratis.no/");
        pages.Exists(page => page.Url.Value == "https://cratis.direct/").ShouldBeTrue();
        pages.Exists(page => SourceKinds.Of(page.Url) == SourceKind.Sample).ShouldBeTrue();
        responses.ReadmeRequests.ShouldEqual(0);
    }

    [Fact]
    public async Task should_include_readmes_and_retain_their_snapshot_on_failure()
    {
        using var responses = new Responses(false);
        using var client = new HttpClient(responses) { BaseAddress = new Uri("https://cratis.io/") };
        var options = Options.Create(new PrompterOptions());
        DisableNewSources(options.Value);
        options.Value.Ingestion.Readmes.Enabled = true;
        options.Value.Ingestion.ReleaseNotes.Enabled = false;
        options.Value.Ingestion.Samples.Enabled = false;
        var source = Create(client, options);
        var count = 0;
        await foreach (var page in source.GetPages(TestContext.Current.CancellationToken)) count++;
        count.ShouldEqual(1);
        responses.ReadmeRequests.ShouldEqual(1);
        source.FailedSources.ShouldContainOnly("https://github.com/Cratis/");
    }

    static void DisableNewSources(PrompterOptions options)
    {
        options.Ingestion.Sites.Company.Enabled = false;
        options.Ingestion.Sites.Studio.Enabled = false;
        options.Ingestion.Sites.Direct.Enabled = false;
        options.Ingestion.Readmes.Enabled = false;
    }

    static CorpusSources Create(HttpClient client, Microsoft.Extensions.Options.IOptions<PrompterOptions> options) => new(
        new(client, options, NullLogger<DocsSite>.Instance),
        new(client, options, NullLogger<ReleaseNotes>.Instance),
        new(client, options, NullLogger<SampleFiles>.Instance),
        new(client, NullLogger<CratisSites>.Instance),
        new(client, options, NullLogger<RepositoryReadmes>.Instance),
        options);

    sealed class Responses(bool failSamples) : HttpMessageHandler
    {
        public int ReleaseRequests;
        public int SampleRequests;
        public int ReadmeRequests;
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
            if (url.Host == "raw.githubusercontent.com" && url.AbsolutePath.Contains("/.github/", StringComparison.Ordinal))
            {
                ReadmeRequests++;
                status = HttpStatusCode.Forbidden;
            }
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = url.Host == "cratis.direct" ? new StringContent("<h1>Direct</h1><p>Deployment</p>", System.Text.Encoding.UTF8, "text/html") : new StringContent(body)
            });
        }
    }
}
