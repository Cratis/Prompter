// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_many_clients_search_concurrently : Specification
{
    readonly CountingAuthentication _authentication = new();
    readonly BlockingResponse _wire = new();
    int _beforeRelease;

    async Task Because()
    {
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var limits = new PublicIssueSearchLimits(clock);
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        _authentication.InnerHandler = _wire;
        using var client = new HttpClient(_authentication) { BaseAddress = new("https://api.github.com/") };
        var searches = Enumerable.Range(0, 30).Select(index => index % 2 == 0
            ? new RelatedIssues(client, options, clock, limits).FindRelated("arc", "Arc broken")
            : new PublicRelatedIssues(client, options, clock, limits).Find("Arc broken")).ToArray();
        await _wire.Full.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, TestContext.Current.CancellationToken);
        _beforeRelease = _wire.Calls;
        _wire.Release.SetResult();
        await Task.WhenAll(searches);
    }

    void Destroy()
    {
        _authentication.Dispose();
        _wire.Dispose();
    }

    [Fact] void should_never_exceed_eight_concurrent_gets() => _beforeRelease.ShouldEqual(8);
    [Fact] void should_not_send_second_buckets_after_quota_exhaustion() => _wire.Calls.ShouldEqual(8);
    [Fact] void should_not_enter_authentication_for_denied_gets() => _authentication.Calls.ShouldEqual(8);

    sealed class CountingAuthentication : DelegatingHandler
    {
        int _calls;
        public int Calls => _calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return base.SendAsync(request, cancellationToken);
        }
    }

    sealed class BlockingResponse : HttpMessageHandler
    {
        int _calls;
        public int Calls => _calls;
        public TaskCompletionSource Full { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == 8) Full.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"incomplete_results\":false,\"items\":[]}") };
        }
    }
}
