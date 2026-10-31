// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_SampleFiles.when_reading;

public class and_a_source_is_incomplete : Specification
{
    [Theory]
    [InlineData("truncated")]
    [InlineData("json")]
    [InlineData("tree")]
    [InlineData("raw")]
    [InlineData("timeout")]
    public async Task should_fail_closed(string failure)
    {
        using var responses = new Responses(failure);
        using var client = new HttpClient(responses);
        var source = new SampleFiles(client, Options.Create(new PrompterOptions()), NullLogger<SampleFiles>.Instance);
        var error = await Catch.Exception(async () => { await foreach (var page in source.GetPages()) { } });
        error.ShouldBeOfExactType<IncompleteCrawl>();
    }

    sealed class Responses(string failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (failure == "timeout") throw new TaskCanceledException();
            var status = failure == "tree" || request.RequestUri!.Host == "raw.githubusercontent.com" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            var body = failure switch
            {
                "truncated" => "{\"truncated\":true,\"tree\":[]}",
                "json" => "invalid json",
                _ => "{\"truncated\":false,\"tree\":[{\"type\":\"blob\",\"path\":\"README.md\",\"size\":10}]}"
            };
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
