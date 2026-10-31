// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>A fake anonymous GitHub search: replays queued responses and records requests, with no network.</summary>
public sealed class FakeGitHubSearch : HttpMessageHandler
{
    readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public IList<Uri> Requests { get; } = [];

    public IList<bool> HadAuthorization { get; } = [];

    public FakeGitHubSearch Respond(string json, HttpStatusCode status = HttpStatusCode.OK, params (string Name, string Value)[] headers)
    {
        _responses.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            foreach (var (name, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return Task.FromResult(response);
        });
        return this;
    }

    public FakeGitHubSearch Hang()
    {
        _responses.Enqueue(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage();
        });
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        HadAuthorization.Add(request.Headers.Authorization is not null);
        if (_responses.Count == 0)
        {
            Respond(string.Empty, HttpStatusCode.InternalServerError);
        }

        return _responses.Dequeue()(request, cancellationToken);
    }
}
