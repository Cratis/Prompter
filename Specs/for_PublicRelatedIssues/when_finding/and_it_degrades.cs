// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_PublicRelatedIssues.when_finding;

public class and_it_degrades : given.a_public_search
{
    IReadOnlyList<RelatedIssue> _rateLimited = null!;
    IReadOnlyList<RelatedIssue> _afterLimit = null!;
    int _callsAfterLimit;

    void Establish() => Handler.Respond("{}", HttpStatusCode.Forbidden, ("Retry-After", "120"));

    async Task Because()
    {
        _rateLimited = await Related.Find("Chronicle projection fails", TestContext.Current.CancellationToken);
        _afterLimit = await Related.Find("Arc command fails", TestContext.Current.CancellationToken);
        _callsAfterLimit = Handler.Requests.Count;
    }

    [Fact] void should_show_nothing_when_rate_limited() => _rateLimited.Count.ShouldEqual(0);
    [Fact] void should_not_retry() => _callsAfterLimit.ShouldEqual(1);
    [Fact] void should_honor_the_cooldown_without_calling() => _afterLimit.Count.ShouldEqual(0);
}
