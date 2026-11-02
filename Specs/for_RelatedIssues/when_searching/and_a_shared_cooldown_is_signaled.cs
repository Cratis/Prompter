// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RelatedIssues.when_searching;

public class and_a_shared_cooldown_is_signaled : Specification
{
    [Theory]
    [InlineData(403)]
    [InlineData(429)]
    public async Task should_cool_down_both_modes_and_clamp_the_hint(int status)
    {
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var limits = new PublicIssueSearchLimits(clock);
        var options = Options.Create(new PrompterOptions());
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        using var handler = new LimitedResponse((HttpStatusCode)status);
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.github.com/") };
        await new RelatedIssues(client, options, clock, limits).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        await new PublicRelatedIssues(client, options, clock, limits).Find("Arc broken", TestContext.Current.CancellationToken);
        handler.Calls.ShouldEqual(1);
        clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        await new RelatedIssues(client, options, clock, limits).FindRelated("arc", "Arc broken", TestContext.Current.CancellationToken);
        handler.Calls.ShouldEqual(1);
        clock.Advance(TimeSpan.FromSeconds(1));
        await new PublicRelatedIssues(client, options, clock, limits).Find("Arc broken", TestContext.Current.CancellationToken);
        handler.Calls.ShouldEqual(3);
    }

    sealed class LimitedResponse(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(Calls == 1 ? status : HttpStatusCode.OK)
            {
                Content = new StringContent("{\"incomplete_results\":false,\"items\":[]}")
            };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(24));
            return Task.FromResult(response);
        }
    }
}
