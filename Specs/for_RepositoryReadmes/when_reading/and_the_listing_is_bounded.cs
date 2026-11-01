// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_the_listing_is_bounded : given.a_readme_source
{
    void Establish()
    {
        _options.Ingestion.Readmes.MaxRepositories = 101;
        _listing = "[" + string.Join(',', Enumerable.Repeat("{\"name\":\"Arc\",\"default_branch\":\"main\",\"private\":false}", 100)) + "]";
    }
    Task Because() => Read();
    [Fact] void should_page_deduplicate_and_stop_at_the_repository_bound() => _requests.ShouldEqual(["https://api.github.com/orgs/Cratis/repos?type=public&per_page=100&page=1", "https://raw.githubusercontent.com/Cratis/Arc/main/README.md", "https://api.github.com/orgs/Cratis/repos?type=public&per_page=100&page=2"]);
    [Fact] void should_yield_only_one_copy() => _pages.Count.ShouldEqual(1);
}
