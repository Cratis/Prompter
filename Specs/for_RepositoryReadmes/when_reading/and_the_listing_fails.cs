// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_the_listing_fails : given.a_readme_source
{
    void Establish() => _listingStatus = HttpStatusCode.Forbidden;
    Task Because() => Read();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_yield_partial_content() => _pages.ShouldBeEmpty();
}
