// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_PublicRelatedIssues.when_finding;

public class and_it_is_unavailable : given.a_public_search
{
    IReadOnlyList<RelatedIssue> _disabled = null!;
    IReadOnlyList<RelatedIssue> _empty = null!;
    IReadOnlyList<RelatedIssue> _hung = null!;
    Exception? _cancelled;

    async Task Because()
    {
        Config.GitHub.PublicRelatedIssuesEnabled = false;
        _disabled = await Related.Find("Chronicle projection fails", TestContext.Current.CancellationToken);
        Config.GitHub.PublicRelatedIssuesEnabled = true;
        _empty = await Related.Find("12 34 !!", TestContext.Current.CancellationToken);
        Handler.Hang();
        _hung = await Related.Find("Chronicle projection fails", TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _cancelled = await Catch.Exception(async () => await Related.Find("Arc command fails", cancelled.Token));
    }

    [Fact] void should_be_off_when_disabled() => _disabled.Count.ShouldEqual(0);
    [Fact] void should_not_search_without_keywords() => _empty.Count.ShouldEqual(0);
    [Fact] void should_not_call_when_disabled_or_empty() => Handler.Requests.Count.ShouldEqual(1);
    [Fact] void should_give_up_after_the_deadline() => _hung.Count.ShouldEqual(0);
    [Fact] void should_propagate_caller_cancellation() => _cancelled.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_default_to_on() => new PrompterOptions().GitHub.PublicRelatedIssuesEnabled.ShouldBeTrue();
}
