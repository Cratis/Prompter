// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_ReleaseNotes.when_reading;

public class and_github_cannot_be_read : Specification
{
    [Theory]
    [InlineData(403, "[]")]
    [InlineData(429, "[]")]
    [InlineData(503, "[]")]
    [InlineData(200, "invalid json")]
    [InlineData(200, "null")]
    [InlineData(0, "")]
    public async Task should_preserve_the_previous_corpus(int status, string body)
    {
        using var responses = new Responses(status, body);
        using var client = new HttpClient(responses);
        var source = new ReleaseNotes(client, Options.Create(new PrompterOptions()), NullLogger<ReleaseNotes>.Instance);
        var error = await Catch.Exception(async () => { await foreach (var page in source.GetPages()) { } });
        error.ShouldBeOfExactType<IncompleteCrawl>();
    }

    [Fact] void should_render_the_cli_display_name() => ReleaseNotes.Markdown("cli", new("v1.2.3", null, "Changes", false, false, null, "unused")).ShouldContain("# CLI v1.2.3");

    sealed class Responses(int status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => status == 0
            ? throw new TaskCanceledException()
            : Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
    }
}
