// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_the_sitemap_lists_another_host : Specification
{
    readonly List<string> _requests = [];
    readonly List<DocsPage> _pages = [];
    async Task Because()
    {
        using var responses = new Responses(_requests);
        using var client = new HttpClient(responses) { BaseAddress = new("https://cratis.io/") };
        var site = new DocsSite(client, Options.Create(new PrompterOptions()), NullLogger<DocsSite>.Instance);
        await foreach (var page in site.GetPages()) _pages.Add(page);
    }
    [Fact] void should_fetch_only_docs_and_the_www_twin() => _requests.ShouldEqual(["https://cratis.io/sitemap-0.xml", "https://cratis.io/a.md", "https://www.cratis.io/b.md"]);
    [Fact] void should_never_yield_untrusted_hosts() => _pages.Count.ShouldEqual(2);

    sealed class Responses(List<string> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests.Add(request.RequestUri!.AbsoluteUri);
            var body = request.RequestUri.AbsolutePath == "/sitemap-0.xml"
                ? "<loc>https://evil.invalid/x</loc><loc>https://cratis.io.evil.invalid/y</loc><loc>https://cratis.io/a</loc><loc>https://www.cratis.io/b</loc>"
                : "# Docs\n\nDocumented behavior.";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
