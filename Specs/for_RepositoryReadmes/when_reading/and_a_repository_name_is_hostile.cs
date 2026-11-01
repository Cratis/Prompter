// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_a_repository_name_is_hostile : given.a_readme_source
{
    void Establish() => _listing = """
        [{"name":"../x","default_branch":"main","private":false},
         {"name":"a/b","default_branch":"main","private":false},
         {"name":"%2e%2e","default_branch":"main","private":false},
         {"name":"..","default_branch":"main","private":false},
         {"name":"Arc","default_branch":"feature/x","private":false}]
        """;
    Task Because() => Read();
    [Fact] void should_never_request_a_hostile_path() => _requests.Count.ShouldEqual(1);
    [Fact] void should_not_yield_hostile_repositories() => _pages.ShouldBeEmpty();
    [Fact] void should_complete() => _error.ShouldBeNull();
}
