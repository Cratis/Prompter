// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_the_endpoint_is_saturated : given.a_verified_delivery
{
    int? _validStatus;
    int? _invalidStatus;
    string _retry = string.Empty;

    void Establish()
    {
        Options.Protection.WebhookPerMinute = 0;
        Options.GitHub.WebhookSecret = "test-placeholder";
        Create();
    }

    async Task Because()
    {
        var valid = Request(true);
        _validStatus = ((IStatusCodeHttpResult)await Receive(valid)).StatusCode;
        _retry = valid.HttpContext.Response.Headers.RetryAfter.ToString();
        _invalidStatus = ((IStatusCodeHttpResult)await Receive(Request(false))).StatusCode;
    }

    HttpRequest Request(bool signed)
    {
        var body = Encoding.UTF8.GetBytes("""{"action":"opened","repository":{"full_name":"Cratis/Arc"},"issue":{"number":1,"title":"question","body":"details"}}""");
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(body);
        if (signed) context.Request.Headers[WebhookAuth.SignatureHeader] = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-placeholder"), body));
        return context.Request;
    }

    async Task<IResult> Receive(HttpRequest request)
    {
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var processing = new WebhookProcessing(options, Clock, NullLogger<WebhookProcessing>.Instance);
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var endpoint = typeof(PrompterEndpoints).GetMethod("ReceiveGitHubEvent", BindingFlags.Static | BindingFlags.NonPublic)!;
        return await (Task<IResult>)endpoint.Invoke(null, [request, options, Webhook, processing, Fakes.Switch.Running(), lifetime, NullLoggerFactory.Instance, CancellationToken.None])!;
    }

    [Fact] void should_report_overload() => _validStatus.ShouldEqual(429);
    [Fact] void should_suggest_a_bounded_redelivery_interval() => _retry.ShouldEqual("60");
    [Fact] void should_preserve_signature_rejection() => _invalidStatus.ShouldEqual(401);
    [Fact] void should_not_claim_any_issue() => Deliveries.ReceivedCalls().Count().ShouldEqual(0);
    [Fact]
    void should_not_answer_or_notify()
    {
        Answers.ReceivedCalls().ShouldBeEmpty();
        Channel.ReceivedCalls().ShouldBeEmpty();
    }
}
