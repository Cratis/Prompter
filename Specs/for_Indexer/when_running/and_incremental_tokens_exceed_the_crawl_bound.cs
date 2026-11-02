// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_incremental_tokens_exceed_the_crawl_bound : given.an_unending_crawl
{
    void Establish() => Options.Ingestion.MaxIncrementalTokens = 1;

    Task Because() => RunUntilRejected();

    [Fact] void should_stop_before_accumulating_an_unbounded_plan() => Pages.Count.ShouldEqual(1);
    [Fact] void should_report_a_budget_rejection() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
