// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_the_lookup_is_unavailable : Specification
{
    [Theory]
    [InlineData(401, "{}")]
    [InlineData(403, "{}")]
    [InlineData(429, "{}")]
    [InlineData(503, "{}")]
    [InlineData(200, "not json")]
    [InlineData(200, "{}")]
    [InlineData(200, "{\"incomplete_results\":true,\"items\":[]}")]
    [InlineData(200, "{\"incomplete_results\":false,\"items\":[{\"number\":\"oops\"}]}")]
    [InlineData(200, "{\"incomplete_results\":false,\"items\":[{\"number\":-1}]}")]
    [InlineData(200, "{\"incomplete_results\":false,\"items\":[{\"number\":1,\"state\":\"closed\",\"closed_at\":\"2000-01-01T00:00:00Z\",\"repository_url\":\"https://api.github.com/repos/Cratis/Arc\"}]}")]
    public async Task should_return_no_hints(int status, string body)
    {
        using var handler = new Response((HttpStatusCode)status, body);
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.github.com/") };
        var result = await new RelatedIssues(client, Configured(), TimeProvider.System).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_fail_closed_without_credentials()
    {
        using var handler = new Response(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        var result = await new RelatedIssues(client, Options.Create(new PrompterOptions()), TimeProvider.System).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
        handler.Calls.ShouldEqual(0);
    }

    [Theory]
    [InlineData("unknown", "Arc broken")]
    [InlineData("arc", " ")]
    [InlineData("arc", "repo:evil/other OR NOT AND")]
    public async Task should_not_search_without_a_known_product_and_keywords(string product, string text)
    {
        using var handler = new Response(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        await new RelatedIssues(client, Configured(), TimeProvider.System).FindRelated(product, text, TestContext.Current.CancellationToken);
        handler.Calls.ShouldEqual(0);
    }

    [Fact]
    public async Task should_reject_a_mapping_outside_the_installation()
    {
        using var handler = new Response(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        var options = Configured();
        options.Value.GitHub.Repositories["arc"] = "NotApproved";
        await new RelatedIssues(client, options, TimeProvider.System).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        handler.Calls.ShouldEqual(0);
    }

    [Theory]
    [InlineData("arc")]
    [InlineData("unknown")]
    public async Task should_propagate_caller_cancellation(string product)
    {
        using var handler = new Response(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RelatedIssues(client, Configured(), TimeProvider.System).FindRelated(product, "Arc broken", cancellation.Token));
        handler.Calls.ShouldEqual(0);
    }

    [Fact]
    public async Task should_bound_response_bytes()
    {
        using var handler = new Response(HttpStatusCode.OK, new string(' ', 65537));
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.github.com/") };
        var result = await new RelatedIssues(client, Configured(), TimeProvider.System).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    static IOptions<PrompterOptions> Configured()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        return options;
    }

    sealed class Response(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
