// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Telemetry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Prompter.Specs.for_ObservedIndexer.when_indexing;

public class and_the_caller_cancels : Specification
{
    [Fact]
    async Task should_record_cancellation_and_keep_it_distinct_from_failure()
    {
        var inner = Substitute.For<IIndexer>();
        var history = Substitute.For<IIndexHistory>();
        inner.Run(Arg.Any<IndexRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());
        var indexer = new ObservedIndexer(inner, history);

        await Assert.ThrowsAsync<OperationCanceledException>(() => indexer.Run(cancellationToken: TestContext.Current.CancellationToken));

        await history.Received(1).Complete("cancelled", null, Arg.Any<double>());
        await history.DidNotReceive().Complete("failed", Arg.Any<IndexRun>(), Arg.Any<double>());
    }
}
