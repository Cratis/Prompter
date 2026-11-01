// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_parent_is_allowed_for_drafting : given.an_issue_interaction
{
    async Task Because() => await Draft(7);

    [Fact] void should_draft_the_description() => Drafting.Received(1).Draft("A reproducible problem", null, Arg.Any<CancellationToken>());
    [Fact] void should_search_the_routed_repository() => Issues.Received(1).FindSimilar("Chronicle", "Title", Arg.Any<CancellationToken>());
    [Fact] void should_not_file_without_confirmation() => Issues.DidNotReceive().File(Arg.Any<Cratis.Prompter.GitHub.PendingIssue>(), Arg.Any<CancellationToken>());
}
