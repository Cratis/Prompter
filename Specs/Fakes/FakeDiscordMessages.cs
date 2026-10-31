// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>An offline Discord REST endpoint that serves one question message, or 404.</summary>
public sealed class FakeDiscordMessages : HttpMessageHandler
{
    /// <summary>Gets or sets the question author, or null when the message is gone.</summary>
    public ulong? Author { get; set; }

    /// <summary>Gets or sets the question content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Creates a REST client that talks only to this fake.</summary>
    /// <returns>The client.</returns>
    public RestClient Client() => new(configuration: new() { RequestHandler = new RestRequestHandler(this, false) });

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Author is not { } author)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"code\":10008,\"message\":\"Unknown Message\"}", Encoding.UTF8, "application/json") });
        }

        var content = System.Text.Json.JsonSerializer.Serialize(Content);
        var json = $$"""
            {"id":"4","channel_id":"2","author":{"id":"{{author}}","username":"asker","discriminator":"0","avatar":null,"global_name":null},"content":{{content}},"timestamp":"2026-06-05T12:00:00+00:00","edited_timestamp":null,"tts":false,"mention_everyone":false,"mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"pinned":false,"type":0}
            """;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
