// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Anthropic;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Owns the SDK and HTTP lifetimes with hidden retries disabled.
/// </summary>
public sealed class AnthropicConnection : IChatClient
{
    readonly AnthropicTransport _transport;
    readonly HttpClient _http;
    readonly AnthropicClient _sdk;
    readonly IChatClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicConnection"/> class.
    /// </summary>
    /// <param name="key">The account key, or empty to retain SDK environment configuration.</param>
    /// <param name="httpMessageHandler">Optional offline transport for wire-level specifications.</param>
    public AnthropicConnection(string key, HttpMessageHandler? httpMessageHandler = null)
    {
        _transport = new() { InnerHandler = httpMessageHandler ?? new HttpClientHandler() };
        _http = new(_transport);
        _sdk = new()
        {
            ApiKey = key.Length > 0 ? key : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"),
            MaxRetries = 0,
            HttpClient = _http
        };
        _client = _sdk.AsIChatClient();
    }

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        _client.GetResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        _client.GetStreamingResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => _client.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose()
    {
        _client.Dispose();
        _sdk.Dispose();
        _http.Dispose();
        _transport.Dispose();
    }
}
