// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.given;

public class a_readme_source : Specification
{
    internal readonly List<string> _requests = [];
    internal readonly List<DocsPage> _pages = [];
    internal PrompterOptions _options = null!;
    internal string _listing = "[]";
    internal HttpStatusCode _listingStatus = HttpStatusCode.OK;
    internal HttpStatusCode _rawStatus = HttpStatusCode.OK;
    internal bool _authorized;
    internal bool _identified = true;
    internal Exception? _error;
    HttpClient _client = null!;
    Responses _handler = null!;
    RepositoryReadmes _source = null!;

    internal async Task Read() => _error = await Catch.Exception(async () =>
    {
        await foreach (var page in _source.GetPages()) _pages.Add(page);
    });

    void Establish()
    {
        _options = new();
        _options.Ingestion.Readmes.IncludeProfile = false;
        _handler = new(this);
        _client = new(_handler);
        _source = new(_client, Options.Create(_options), NullLogger<RepositoryReadmes>.Instance);
    }

    void Destroy()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    sealed class Responses(a_readme_source context) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            context._requests.Add(request.RequestUri!.AbsoluteUri);
            context._authorized |= request.Headers.Authorization is not null;
            context._identified &= request.Headers.UserAgent.ToString() == "Cratis-Prompter" && request.Headers.Accept.ToString() == "application/vnd.github+json";
            var listing = request.RequestUri.Host == "api.github.com";
            return Task.FromResult(new HttpResponseMessage(listing ? context._listingStatus : context._rawStatus)
            {
                Content = new StringContent(listing ? context._listing : "# Repository\n\nUseful public facts.")
            });
        }
    }
}
