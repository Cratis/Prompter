// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_a_page_is_too_large : given.a_site
{
    void Establish()
    {
        _site.Sitemap = string.Empty;
        _site.Pages = ["/"];
        _responses["https://cratis.no/"] = (HttpStatusCode.OK, "text/html", new string('x', 5000));
    }
    Task Because() => Read();
    [Fact] void should_fail_without_yielding_partial_content() => _error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_yield_a_page() => _pages.ShouldBeEmpty();

    [Fact]
    async Task should_bound_streams_without_a_declared_length()
    {
        using var content = new StreamContent(new MemoryStream(new byte[5000]));
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var error = await Catch.Exception(() => BoundedContent.Read(response, 4096, TestContext.Current.CancellationToken));
        error.ShouldBeOfExactType<IncompleteCrawl>();
    }

    [Fact]
    async Task should_reject_non_success_statuses()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway);
        var error = await Catch.Exception(() => BoundedContent.Read(response, 4096, TestContext.Current.CancellationToken));
        error.ShouldBeOfExactType<IncompleteCrawl>();
    }
}
