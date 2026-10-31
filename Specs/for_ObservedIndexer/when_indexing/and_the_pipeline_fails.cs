// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Telemetry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Prompter.Specs.for_ObservedIndexer.when_indexing;

public class and_the_pipeline_fails : Specification
{
    [Fact]
    async Task should_persist_failure_without_swallowing_the_job_error()
    {
        var inner = Substitute.For<IIndexer>();
        var history = Substitute.For<IIndexHistory>();
        var error = new IndexAlreadyRunning();
        inner.Run(Arg.Any<IndexRequest>(), Arg.Any<CancellationToken>()).ThrowsAsync(error);
        var indexer = new ObservedIndexer(inner, history);

        var caught = await Catch.Exception(() => indexer.Run(cancellationToken: TestContext.Current.CancellationToken));

        caught.ShouldBeSame(error);
        Received.InOrder(() =>
        {
            history.Attempt(Arg.Any<CancellationToken>());
            history.Complete("failed", null, Arg.Any<double>());
        });
    }
}
