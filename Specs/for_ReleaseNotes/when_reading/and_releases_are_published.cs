// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_ReleaseNotes.when_reading;

public class and_releases_are_published : Specification
{
    readonly List<DocsPage> _pages = [];
    Responses _responses = null!;
    HttpClient _client = null!;

    void Establish() => _client = new HttpClient(_responses = new Responses());

    async Task Because()
    {
        var options = new PrompterOptions();
        options.Ingestion.ReleaseNotes.Repositories = ["Chronicle"];
        var source = new ReleaseNotes(_client, Options.Create(options), NullLogger<ReleaseNotes>.Instance);
        await foreach (var page in source.GetPages()) _pages.Add(page);
    }

    void Destroy()
    {
        _client.Dispose();
        _responses.Dispose();
    }

    [Fact] void should_keep_only_supported_bounded_published_notes() => _pages.Count.ShouldEqual(1);
    [Fact] void should_keep_the_release_link() => _pages[0].Url.Value.ShouldEqual("https://github.com/Cratis/Chronicle/releases/tag/v1.2.3");
    [Fact] void should_synthesize_the_title_and_body() => _pages[0].Markdown.ShouldContain("# Chronicle v1.2.3\n\nPublished changes.");
    [Fact] void should_read_the_version_metadata() => _pages[0].Metadata.ProductVersion.ShouldEqual("1.2.3");
    [Fact] void should_read_the_publication_date() => _pages[0].Metadata.PageLastModified.ShouldEqual(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
    [Fact] void should_identify_the_client() => _responses.UserAgent.ShouldEqual("Cratis-Prompter");
    [Fact] void should_request_the_github_media_type() => _responses.Accept.ShouldEqual("application/vnd.github+json");
    [Fact] void should_never_authorize() => _responses.Authorization.ShouldBeNull();
    [Fact] void should_bound_the_request() => _responses.Query.ShouldEqual("?per_page=20");

    sealed class Responses : HttpMessageHandler
    {
        public string? UserAgent;
        public string? Accept;
        public string? Authorization;
        public string? Query;
        static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UserAgent = request.Headers.UserAgent.ToString();
            Accept = request.Headers.Accept.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Query = request.RequestUri!.Query;
            var notes = new[]
            {
                new ReleaseNote("v1.2.3", null, "Published changes.", false, false, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), "https://github.com/Cratis/Chronicle/releases/tag/v1.2.3"),
                new ReleaseNote("v2.0.0", null, "Draft", true, false, null, "https://github.com/Cratis/Chronicle/releases/tag/v2.0.0"),
                new ReleaseNote("v2.0.0", null, "Preview", false, true, null, "https://github.com/Cratis/Chronicle/releases/tag/v2.0.0"),
                new ReleaseNote("v2.0.0", null, new string('x', 20_001), false, false, null, "https://github.com/Cratis/Chronicle/releases/tag/v2.0.0"),
                new ReleaseNote("v2.0.0", null, string.Empty, false, false, null, "https://github.com/Cratis/Chronicle/releases/tag/v2.0.0"),
                new ReleaseNote("v2.0.0", null, "Wrong URL", false, false, null, "https://other.example/releases/tag/v2.0.0")
            };
            var json = System.Text.Json.JsonSerializer.Serialize(notes, _jsonOptions);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
