// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ObservedIndexer.when_indexing;

public class and_the_pipeline_succeeds : Specification
{
    [Fact]
    async Task should_persist_the_actual_summary_after_completion()
    {
        var inner = Substitute.For<IIndexer>();
        var history = Substitute.For<IIndexHistory>();
        var summary = new IndexRun(2, 10, 5, 0, TimeSpan.FromSeconds(1));
        inner.Run(Arg.Any<IndexRequest>(), Arg.Any<CancellationToken>()).Returns(summary);
        var indexer = new ObservedIndexer(inner, history);

        var result = await indexer.Run(cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeSame(summary);
        await history.Received(1).Complete("success", summary, Arg.Any<double>());
        await history.DidNotReceive().Complete("failed", Arg.Any<IndexRun>(), Arg.Any<double>());
    }
}
