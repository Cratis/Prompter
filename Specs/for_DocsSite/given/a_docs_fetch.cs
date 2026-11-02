// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_DocsSite.given;

public class a_docs_fetch : Specification
{
    HttpClient _client = null!;
    Responses _responses = null!;

    protected PrompterOptions Options { get; } = new() { DocsSiteUrl = "https://docs.invalid/" };
    protected IList<Uri> Requests { get; } = new List<Uri>();
    protected Func<Uri, HttpResponseMessage> Respond { get; set; } = _ => Text("# Page");
    protected Exception? Error { get; private set; }
    protected IList<DocsPage> Pages { get; } = new List<DocsPage>();
    DocsSite Site { get; set; } = null!;

    protected static HttpResponseMessage Text(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text) };

    protected static HttpResponseMessage Redirect(string destination) => new(HttpStatusCode.MovedPermanently)
    {
        Headers = { Location = new Uri(destination, UriKind.RelativeOrAbsolute) }
    };

    protected async Task Crawl(CancellationToken cancellationToken = default)
    {
        Error = await Catch.Exception(async () =>
        {
            await foreach (var page in Site.GetPages(cancellationToken)) Pages.Add(page);
        });
    }

    void Establish()
    {
        _responses = new Responses(uri =>
        {
            Requests.Add(uri);
            return Respond(uri);
        });
        _client = new HttpClient(_responses, disposeHandler: false) { BaseAddress = new Uri(Options.DocsSiteUrl) };
        Site = new DocsSite(_client, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<DocsSite>.Instance);
    }

    void Destroy()
    {
        _client.Dispose();
        _responses.Dispose();
    }

    sealed class Responses(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request.RequestUri!));
    }
}
