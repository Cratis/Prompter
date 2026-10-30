// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_confirming;

public class and_cancel_races_with_filing : Specification
{
    int _winners;

    async Task Because()
    {
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(new("Cratis", "Chronicle", "Title", "Body", []), scope, _ => Task.FromResult(4UL));
        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            if (pending.Take(token, scope, 4) is not null)
            {
                Interlocked.Increment(ref _winners);
            }
        })));
    }

    [Fact] void should_allow_exactly_one_consumer_for_either_action() => _winners.ShouldEqual(1);
}
