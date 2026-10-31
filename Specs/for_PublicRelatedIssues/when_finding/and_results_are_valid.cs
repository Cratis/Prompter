// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_PublicRelatedIssues.when_finding;

public class and_results_are_valid : given.a_public_search
{
    IReadOnlyList<RelatedIssue> _issues = null!;
    IReadOnlyList<RelatedIssue> _again = null!;

    void Establish() => Handler
        .Respond(Items((1, "Arc", "open", null, false), (2, "Arc", "open", null, true), (3, "Evil", "open", null, false), (4, "Chronicle", "closed", null, false)))
        .Respond(Items((5, "Chronicle", "closed", "2026-05-20T00:00:00Z", false), (6, "CLI", "closed", "2025-01-01T00:00:00Z", false), (7, "Arc", "open", null, false), (8, "Arc", "closed", "2026-05-21T00:00:00Z", false), (9, "Arc", "closed", "2026-05-22T00:00:00Z", false)));

    async Task Because()
    {
        _issues = await Related.Find("Chronicle projection fails", TestContext.Current.CancellationToken);
        _again = await Related.Find("chronicle PROJECTION fails", TestContext.Current.CancellationToken);
    }

    [Fact] void should_keep_at_most_three_links() => _issues.Count.ShouldEqual(3);
    [Fact] void should_prefer_open_issues() => _issues[0].Number.ShouldEqual(1);
    [Fact] void should_reject_pull_requests_and_wrong_repositories() => _issues.Select(issue => issue.Number).Intersect([2, 3]).ShouldBeEmpty();
    [Fact] void should_reject_wrong_state_and_old_closures() => _issues.Select(issue => issue.Number).Intersect([4, 6, 7]).ShouldBeEmpty();
    [Fact] void should_mark_recently_closed() => _issues[1].RecentlyClosed.ShouldBeTrue();
    [Fact] void should_canonicalize_urls_locally() => _issues[0].Url.ShouldEqual("https://github.com/Cratis/Arc/issues/1");
    [Fact] void should_name_the_repository() => _issues[0].Repository.ShouldEqual("Arc");
    [Fact] void should_use_two_requests_at_most() => Handler.Requests.Count.ShouldEqual(2);
    [Fact] void should_send_no_credentials() => Handler.HadAuthorization.ShouldNotContain(true);
    [Fact] void should_use_fixed_public_qualifiers() => Uri.UnescapeDataString(Handler.Requests[0].Query).ShouldContain("is:issue is:public repo:Cratis/Chronicle repo:Cratis/Arc repo:Cratis/Fundamentals repo:Cratis/Components repo:Cratis/CLI is:open");
    [Fact] void should_serve_the_repeat_from_cache() => _again.Count.ShouldEqual(3);
}
