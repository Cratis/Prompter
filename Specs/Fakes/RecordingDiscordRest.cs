// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>An offline Discord REST endpoint that serves one starter message and records every message sent.</summary>
public sealed class RecordingDiscordRest : HttpMessageHandler
{
    readonly List<JsonElement> _sent = [];

    /// <summary>Gets the JSON body of every message that was posted, in order.</summary>
    public IReadOnlyList<JsonElement> Sent => _sent;

    /// <summary>Gets whether every posted message disables all mentions (empty parse, users and roles lists).</summary>
    public bool MentionedNobody => _sent.TrueForAll(message =>
        message.TryGetProperty("allowed_mentions", out var mentions) &&
        mentions.GetProperty("parse").GetArrayLength() == 0 &&
        mentions.GetProperty("users").GetArrayLength() == 0 &&
        mentions.GetProperty("roles").GetArrayLength() == 0);

    /// <summary>Creates a REST client that talks only to this fake.</summary>
    /// <returns>The client.</returns>
    public RestClient Client() => new(configuration: new() { RequestHandler = new RestRequestHandler(this, false) });

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post && request.Content is not null)
        {
            using var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
            _sent.Add(document.RootElement.Clone());
        }

        const string json = """
            {"id":"70","channel_id":"70","author":{"id":"3","username":"asker","discriminator":"0","avatar":null,"global_name":null},"content":"How do I append an event?","timestamp":"2026-06-05T12:00:00+00:00","edited_timestamp":null,"tts":false,"mention_everyone":false,"mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"pinned":false,"type":0}
            """;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
