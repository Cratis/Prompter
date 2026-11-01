// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_the_organization_lists_repositories : given.a_readme_source
{
    void Establish() => _listing = """
        [{"name":"Arc","default_branch":"main","private":false,"description":"CQRS"},
         {"name":"Old","default_branch":"main","private":false,"archived":true},
         {"name":"Fork","default_branch":"main","private":false,"fork":true},
         {"name":"Private","default_branch":"main","private":true},
         {"name":"Hidden","default_branch":"main","private":false,"visibility":"private"},
         {"name":"Disabled","default_branch":"main","private":false,"disabled":true},
         {"name":".github","default_branch":"main","private":false},
         {"name":"Samples","default_branch":"main","private":false}]
        """;
    Task Because() => Read();
    [Fact] void should_fetch_only_public_active_owned_readmes() => _requests.ShouldEqual(["https://api.github.com/orgs/Cratis/repos?type=public&per_page=100&page=1", "https://raw.githubusercontent.com/Cratis/Arc/main/README.md"]);
    [Fact] void should_use_a_human_readable_citation() => _pages.Single().Url.Value.ShouldEqual("https://github.com/Cratis/Arc/blob/main/README.md");
    [Fact] void should_include_safe_metadata() => _pages.Single().Markdown.ShouldContain("description: \"CQRS\"");
    [Fact] void should_never_authorize() => _authorized.ShouldBeFalse();
    [Fact] void should_identify_the_public_client() => _identified.ShouldBeTrue();
    [Fact] void should_complete() => _error.ShouldBeNull();
}
