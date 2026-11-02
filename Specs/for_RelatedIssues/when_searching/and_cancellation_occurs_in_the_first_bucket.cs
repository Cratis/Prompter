// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_cancellation_occurs_in_the_first_bucket : Specification
{
    int _calls;
    Exception? _error;

    async Task Because()
    {
        using var caller = new CancellationTokenSource();
        using var handler = new CancelingResponse(caller, () => _calls++);
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.github.com/") };
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        var search = new RelatedIssues(client, options, TimeProvider.System, new(TimeProvider.System));
        _error = await Catch.Exception(() => search.FindRelated("arc", "Arc broken", caller.Token));
    }

    [Fact] void should_propagate_caller_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_create_a_second_bucket_request() => _calls.ShouldEqual(1);

    sealed class CancelingResponse(CancellationTokenSource caller, Action called) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            called();
            await caller.CancelAsync();
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"incomplete_results\":false,\"items\":[]}") };
        }
    }
}
