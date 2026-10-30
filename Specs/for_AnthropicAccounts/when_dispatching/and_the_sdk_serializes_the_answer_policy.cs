// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.for_AnthropicAccounts.when_dispatching;

public class and_the_sdk_serializes_the_answer_policy : Specification
{
    ProviderHttp _http = null!;
    AnthropicConnection _primary = null!;
    AnthropicAccounts _client = null!;
    JsonDocument _body = null!;
    ChatResponse _response = null!;

    void Establish()
    {
        _http = new();
        _primary = new AnthropicConnection("dummy-api-key", _http);
        _client = new(_primary, null, new());
    }

    async Task Because()
    {
        _response = await _client.GetResponseAsync([new(ChatRole.User, "dummy question")]);
        _body = JsonDocument.Parse(_http.Body);
    }

    [Fact] void should_send_sonnet_55() => _body.RootElement.GetProperty("model").GetString().ShouldEqual("claude-sonnet-5-5");
    [Fact] void should_send_adaptive_thinking() => _body.RootElement.GetProperty("thinking").GetProperty("type").GetString().ShouldEqual("adaptive");
    [Fact] void should_send_high_effort() => _body.RootElement.GetProperty("output_config").GetProperty("effort").GetString().ShouldEqual("high");
    [Fact] void should_send_bounded_output() => _body.RootElement.GetProperty("max_tokens").GetInt32().ShouldEqual(4096);
    [Fact] void should_omit_temperature() => _body.RootElement.TryGetProperty("temperature", out _).ShouldBeFalse();
    [Fact] void should_capture_input_usage() => _response.Usage!.InputTokenCount.ShouldEqual(5);
    [Fact] void should_capture_output_usage() => _response.Usage!.OutputTokenCount.ShouldEqual(2);

    void Destroy()
    {
        _body?.Dispose();
        _client.Dispose();
        _primary.Dispose();
    }
}
