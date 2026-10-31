// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.Fakes;

public sealed class ProviderHttp(HttpStatusCode status = HttpStatusCode.OK, string? retryAfter = null) : HttpMessageHandler
{
    public string Body { get; private set; } = string.Empty;
    public int Calls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent("""
                {"id":"msg_dummy","type":"message","role":"assistant","model":"claude-sonnet-5-5",
                 "content":[{"type":"text","text":"dummy"}],"stop_reason":"end_turn","stop_sequence":null,
                 "usage":{"input_tokens":5,"output_tokens":2}}
                """)
        };
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        }

        return response;
    }
}
