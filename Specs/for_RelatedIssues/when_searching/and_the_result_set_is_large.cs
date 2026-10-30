// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_the_result_set_is_large : Specification
{
    ManyResults _handler = null!;
    HttpClient _client = null!;
    IReadOnlyList<RelatedIssue> _result = [];

    void Establish()
    {
        _handler = new();
        _client = new(_handler) { BaseAddress = new("https://api.github.com/") };
    }

    async Task Because()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        var text = string.Join(' ', Enumerable.Range(0, 12).Select(index => new string((char)('a' + index), 24)));
        _result = await new RelatedIssues(_client, options, TimeProvider.System).FindRelated("arc", text + "\nprivate payload");
    }

    void Destroy()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    [Fact] void should_cap_the_total_at_six() => _result.Count.ShouldEqual(6);
    [Fact] void should_return_distinct_numbers() => _result.Select(issue => issue.Number).Distinct().Count().ShouldEqual(6);
    [Fact] void should_prioritize_open_issues() => _result.Take(3).All(issue => !issue.RecentlyClosed).ShouldBeTrue();
    [Fact] void should_label_the_closed_bucket() => _result.Skip(3).All(issue => issue.RecentlyClosed).ShouldBeTrue();
    [Fact] void should_not_paginate() => _handler.Queries.Count.ShouldEqual(2);
    [Fact] void should_bound_keywords_to_eight_and_two_hundred_characters() => string.Join(' ', _handler.Queries[0].Split(' ').Skip(3)).Length.ShouldEqual(199);
    [Fact] void should_not_forward_multiline_payloads() => _handler.Queries.TrueForAll(query => !query.Contains("payload", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_request_three_per_bucket() => _handler.ThreePerPage.ShouldBeTrue();

    sealed class ManyResults : HttpMessageHandler
    {
        public List<string> Queries { get; } = [];
        public bool ThreePerPage { get; private set; } = true;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parsed = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
            Queries.Add(parsed["q"]!);
            ThreePerPage &= parsed["per_page"] == "3";
            var closed = Queries.Count == 2;
            var state = closed ? "closed" : "open";
            var date = DateTimeOffset.UtcNow.AddDays(-1).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            var items = Enumerable.Range(closed ? 4 : 1, 8).Select(number =>
                $"{{\"number\":{number},\"state\":\"{state}\",\"closed_at\":\"{date}\",\"repository_url\":\"https://api.github.com/repos/Cratis/Arc\"}}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"incomplete_results\":false,\"items\":[" + string.Join(',', items) + "]}")
            });
        }
    }
}
