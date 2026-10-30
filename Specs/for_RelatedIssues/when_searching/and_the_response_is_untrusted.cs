// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_the_response_is_untrusted : Specification
{
    readonly List<Uri> _requests = [];
    IReadOnlyList<RelatedIssue> _result = [];
    HttpClient _client = null!;
    SearchResponse _handler = null!;

    void Establish()
    {
        _handler = new(_requests);
        _client = new(_handler) { BaseAddress = new("https://api.github.com/") };
    }

    async Task Because()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        var search = new RelatedIssues(_client, options, new FixedClock());
        _result = await search.FindRelated("arc", "Arc repo:evil/other OR \"inject\" broken <@123456789012345678> token=hidden credential", CancellationToken.None);
    }

    void Destroy()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    [Fact] void should_make_only_two_get_searches() => _requests.Count.ShouldEqual(2);
    [Fact] void should_use_fixed_open_qualifiers() => Uri.UnescapeDataString(_requests[0].Query).Contains("repo:Cratis/Arc is:issue is:open", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_bound_the_closed_window() => Uri.UnescapeDataString(_requests[1].Query).Contains("is:closed closed:>=2025-10-03", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_drop_injected_qualifiers() => _requests.TrueForAll(uri => !Uri.UnescapeDataString(uri.Query).Contains("evil", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_drop_identities_and_secret_like_text() => _requests.TrueForAll(uri => !Uri.UnescapeDataString(uri.Query).Contains("hidden", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_exclude_cross_repository_pull_request_and_duplicate_records() => _result.Count.ShouldEqual(2);
    [Fact] void should_prefer_open_issues() => _result[0].RecentlyClosed.ShouldBeFalse();
    [Fact] void should_use_only_canonical_links() => _result[0].Url.ShouldEqual("https://github.com/Cratis/Arc/issues/12");
    [Fact] void should_label_closed_as_recently_closed() => _result[1].RecentlyClosed.ShouldBeTrue();

    sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    sealed class SearchResponse(List<Uri> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Method.ShouldEqual(HttpMethod.Get);
            requests.Add(request.RequestUri!);
            var closed = request.RequestUri!.Query.Contains("is%3Aclosed", StringComparison.Ordinal);
            var items = closed
                ? """{"number":12,"state":"closed","closed_at":"2025-12-15T00:00:00Z","repository_url":"https://api.github.com/repos/Cratis/Arc"},{"number":13,"state":"closed","closed_at":"2025-12-15T00:00:00Z","repository_url":"https://api.github.com/repos/Cratis/Arc"}"""
                : """{"number":12,"state":"open","title":"@everyone [evil](https://evil.invalid)","html_url":"https://evil.invalid","repository_url":"https://api.github.com/repos/Cratis/Arc"},{"number":14,"state":"open","repository_url":"https://api.github.com/repos/Cratis/Other"},{"number":15,"state":"open","pull_request":{},"repository_url":"https://api.github.com/repos/Cratis/Arc"},{"number":12,"state":"open","repository_url":"https://api.github.com/repos/Cratis/Arc"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"incomplete_results\":false,\"items\":[" + items + "]}", Encoding.UTF8, "application/json")
            });
        }
    }
}
