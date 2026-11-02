// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_the_transport_times_out : given.a_docs_fetch
{
    void Establish() => Respond = _ => throw new TaskCanceledException();

    Task Because() => Crawl();

    [Fact] void should_not_report_success() => Error.ShouldBeOfExactType<IncompleteCrawl>();
}
