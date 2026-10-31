// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_PublicRelatedIssues.when_finding;

public class and_responses_are_bad : given.a_public_search
{
    readonly List<int> _counts = [];

    void Establish() => Handler
        .Respond("{\"incomplete_results\":true,\"items\":[]}")
        .Respond("not json")
        .Respond("{}", HttpStatusCode.InternalServerError)
        .Respond(new string(' ', 200 * 1024) + "{}");

    async Task Because()
    {
        foreach (var request in new[] { "alpha one", "beta two", "gamma three", "delta four" })
        {
            _counts.Add((await Related.Find(request, TestContext.Current.CancellationToken)).Count);
        }
    }

    [Fact] void should_show_nothing_for_every_failure() => _counts.ShouldEqual([0, 0, 0, 0]);
}
