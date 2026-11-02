// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_CorpusSources.given;

public class a_composed_indexer : for_Indexer.given.an_indexer
{
    protected const string Documentation = "# Docs\n\nDocumentation.";
    protected const string Auxiliary = "---\nproductVersion: 1.2.3\ndescription: A metadata summary\n---\n# æ\n\n中文";
    readonly Responses _responses = new();
    HttpClient _client = null!;
    protected static int DocumentationBytes => Encoding.UTF8.GetByteCount(Documentation);
    protected static int AuxiliaryBytes => Encoding.UTF8.GetByteCount(Auxiliary);
    protected CorpusSources Corpus { get; private set; } = null!;
    protected int AuxiliaryReads => _responses.AuxiliaryReads;

    protected void Compose()
    {
        _client = new(_responses) { BaseAddress = new("https://cratis.io/") };
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        Options.Ingestion.ReleaseNotes.Enabled = false;
        Options.Ingestion.Samples.Enabled = false;
        Options.Ingestion.Readmes.Enabled = false;
        Options.Ingestion.Sites.Studio.Enabled = false;
        Options.Ingestion.Sites.Direct.Enabled = false;
        var site = Options.Ingestion.Sites.Company;
        site.Enabled = true;
        site.Pages = ["/one", "/two", "/three"];
        site.Sitemap = string.Empty;
        site.LlmsTxt = false;
        site.MarkdownMirrors = true;
        Corpus = new(
            new(_client, options, NullLogger<DocsSite>.Instance),
            new(_client, options, NullLogger<ReleaseNotes>.Instance),
            new(_client, options, NullLogger<SampleFiles>.Instance),
            new(_client, NullLogger<CratisSites>.Instance),
            new(_client, options, NullLogger<RepositoryReadmes>.Instance),
            options);
        Indexer = Create(Corpus);
    }

    void Destroy()
    {
        _client?.Dispose();
        _responses.Dispose();
    }

    sealed class Responses : HttpMessageHandler
    {
        public int AuxiliaryReads { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sitemap = request.RequestUri!.AbsolutePath == "/sitemap-0.xml";
            var auxiliary = request.RequestUri.Host == "cratis.no";
            if (auxiliary) AuxiliaryReads++;
            var content = auxiliary ? Auxiliary : Documentation;
            if (sitemap) content = "<loc>https://cratis.io/arc</loc>";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, sitemap ? "application/xml" : "text/markdown")
            });
        }
    }
}
