// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_a_readme_is_missing : given.a_readme_source
{
    void Establish()
    {
        _listing = """[{"name":"Arc","default_branch":"main","private":false}]""";
        _rawStatus = HttpStatusCode.NotFound;
    }
    Task Because() => Read();
    [Fact] void should_skip_the_missing_file() => _pages.ShouldBeEmpty();
    [Fact] void should_not_try_alternate_files() => _requests.Count.ShouldEqual(2);
    [Fact] void should_complete() => _error.ShouldBeNull();
}
