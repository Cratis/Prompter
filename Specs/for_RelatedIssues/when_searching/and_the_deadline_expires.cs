// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_the_deadline_expires : Specification
{
    SlowResponse _handler = null!;
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
        _result = await new RelatedIssues(_client, options, TimeProvider.System).FindRelated("arc", "Arc broken");
    }

    void Destroy()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    [Fact] void should_fail_soft() => _result.ShouldBeEmpty();
    [Fact] void should_finish_the_first_bucket() => _handler.FirstCompleted.ShouldBeTrue();
    [Fact] void should_cancel_the_second_search_without_retrying() => _handler.Calls.ShouldEqual(2);
    [Fact] void should_cancel_transport() => _handler.LastToken.IsCancellationRequested.ShouldBeTrue();

    sealed class SlowResponse : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool FirstCompleted { get; private set; }
        public CancellationToken LastToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastToken = cancellationToken;
            await Task.Delay(TimeSpan.FromMilliseconds(650), cancellationToken);
            if (Calls == 1) FirstCompleted = true;
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"incomplete_results\":false,\"items\":[]}") };
        }
    }
}
