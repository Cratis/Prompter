// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_public_and_authenticated_clients_share_quota : Specification
{
    int _minute;
    int _hour;
    int _reset;

    async Task Because()
    {
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var limits = new PublicIssueSearchLimits(clock);
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        using var handler = new CountingResponse();
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.github.com/") };
        for (var minute = 0; minute < 6; minute++)
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                if (attempt % 2 == 0) await new RelatedIssues(client, options, clock, limits).FindRelated("arc", "Arc broken");
                else await new PublicRelatedIssues(client, options, clock, limits).Find("Arc broken");
            }
            if (minute == 0) _minute = handler.Calls;
            clock.Advance(TimeSpan.FromMinutes(1));
        }
        _hour = handler.Calls;
        clock.Advance(TimeSpan.FromMinutes(54));
        await new RelatedIssues(client, options, clock, limits).FindRelated("arc", "Arc broken");
        _reset = handler.Calls;
    }

    [Fact] void should_count_each_bucket_in_the_shared_minute_budget() => _minute.ShouldEqual(8);
    [Fact] void should_bound_all_instances_to_forty_gets_per_hour() => _hour.ShouldEqual(40);
    [Fact] void should_refill_the_oldest_hour_window() => _reset.ShouldEqual(42);

    sealed class CountingResponse : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"incomplete_results\":false,\"items\":[]}") });
        }
    }
}
