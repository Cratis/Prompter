// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_CratisSites.given;

public class a_site : Specification
{
    internal readonly List<string> _requests = [];
    internal readonly List<DocsPage> _pages = [];
    internal readonly Dictionary<string, (HttpStatusCode Status, string Type, string Body)> _responses = new(StringComparer.Ordinal);
    internal SiteOptions _site = null!;
    internal Exception? _error;
    internal int _maxBytes = 4096;
    internal CratisSites _source = null!;
    HttpClient _client = null!;
    Responses _handler = null!;

    internal async Task Read() => _error = await Catch.Exception(async () =>
    {
        await foreach (var page in _source.GetPages(_site, _maxBytes)) _pages.Add(page);
    });

    void Establish()
    {
        _site = new() { BaseUrl = "https://cratis.no", Sitemap = "sitemap.xml" };
        _handler = new(_requests, _responses);
        _client = new(_handler);
        _source = new(_client, NullLogger<CratisSites>.Instance);
    }

    void Destroy()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    sealed class Responses(List<string> requests, Dictionary<string, (HttpStatusCode Status, string Type, string Body)> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            requests.Add(url);
            var item = responses.GetValueOrDefault(url, (HttpStatusCode.NotFound, "text/plain", "missing"));
            var response = new HttpResponseMessage(item.Item1) { Content = new StringContent(item.Item3, System.Text.Encoding.UTF8, item.Item2) };
            if ((int)item.Item1 is >= 300 and < 400) response.Headers.Location = new("https://evil.invalid/");
            return Task.FromResult(response);
        }
    }
}
