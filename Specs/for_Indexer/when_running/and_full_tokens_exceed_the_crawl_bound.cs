// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_full_tokens_exceed_the_crawl_bound : given.an_unending_crawl
{
    void Establish() => Options.Ingestion.MaxFullTokens = 1;

    Task Because() => RunUntilRejected(new(Full: true, Confirmation: "FULL REINDEX"));

    [Fact] void should_apply_the_full_mode_bound_during_crawl() => Pages.Count.ShouldEqual(1);
    [Fact] void should_report_a_budget_rejection() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
