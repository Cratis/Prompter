// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_a_page_fetch_fails : Specification
{
    DocsResponses _responses = null!;
    HttpClient _client = null!;
    DocsSite _site = null!;
    Exception? _error;
    int _pages;

    void Establish()
    {
        _responses = new DocsResponses();
        _client = new HttpClient(_responses, disposeHandler: false) { BaseAddress = new Uri("https://cratis.io/") };
        _site = new DocsSite(_client, Options.Create(new PrompterOptions()), NullLogger<DocsSite>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await foreach (var page in _site.GetPages()) _pages++;
    });

    void Destroy()
    {
        _client.Dispose();
        _responses.Dispose();
    }

    [Fact] void should_fail_instead_of_returning_a_partial_success() => _error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_hide_the_failure_after_an_earlier_page() => _pages.ShouldEqual(1);

    sealed class DocsResponses : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.RequestUri!.AbsolutePath switch
            {
                "/sitemap-0.xml" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<urlset><url><loc>https://cratis.io/a</loc></url><url><loc>https://cratis.io/b</loc></url></urlset>") },
                "/a.md" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("# A\n\nAlpha.") },
                _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            };
            return Task.FromResult(response);
        }
    }
}
