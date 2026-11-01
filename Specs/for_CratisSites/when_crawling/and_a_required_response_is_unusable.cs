// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_a_required_response_is_unusable : given.a_site
{
    [Theory]
    [InlineData(HttpStatusCode.OK, "application/json")]
    [InlineData(HttpStatusCode.BadGateway, "text/html")]
    [InlineData(HttpStatusCode.NotFound, "text/html")]
    async Task should_fail_the_site_instead_of_yielding_error_text(HttpStatusCode status, string mediaType)
    {
        _site.Sitemap = string.Empty;
        _site.Pages = ["/"];
        _responses["https://cratis.no/"] = (status, mediaType, "error");
        await Read();
        _error.ShouldBeOfExactType<IncompleteCrawl>();
        _pages.ShouldBeEmpty();
    }

    [Fact]
    async Task should_include_llms_as_plain_text()
    {
        _site.Sitemap = string.Empty;
        _site.LlmsTxt = true;
        _responses["https://cratis.no/llms.txt"] = (HttpStatusCode.OK, "text/plain", "# Company\n\nPublic overview");
        await Read();
        _pages.Single().Url.Value.ShouldEqual("https://cratis.no/llms.txt");
        _error.ShouldBeNull();
    }
}
