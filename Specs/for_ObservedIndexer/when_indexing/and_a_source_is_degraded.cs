// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ObservedIndexer.when_indexing;

public class and_a_source_is_degraded : Specification
{
    [Fact]
    public async Task should_report_failure_after_partial_publication_instead_of_full_success()
    {
        var inner = Substitute.For<IIndexer>();
        var history = Substitute.For<IIndexHistory>();
        var degraded = new DegradedIndex(new(1, 1, 0, 0, TimeSpan.Zero), ["https://github.com/Cratis/Arc/releases/"]);
        inner.Run(Arg.Any<IndexRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IndexRun>(degraded));
        var indexer = new ObservedIndexer(inner, history);
        var error = await Catch.Exception(() => indexer.Run(TestContext.Current.CancellationToken));
        ReferenceEquals(error, degraded).ShouldBeTrue();
        await history.Received(1).Complete("failed", null, Arg.Any<double>());
        await history.DidNotReceive().Complete("success", Arg.Any<IndexRun>(), Arg.Any<double>());
    }
}
