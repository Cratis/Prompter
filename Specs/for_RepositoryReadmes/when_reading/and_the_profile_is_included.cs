// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RepositoryReadmes.when_reading;

public class and_the_profile_is_included : given.a_readme_source
{
    void Establish() => _options.Ingestion.Readmes.IncludeProfile = true;
    Task Because() => Read();
    [Fact] void should_fetch_the_public_profile() => _requests[0].ShouldEqual("https://raw.githubusercontent.com/Cratis/.github/main/profile/README.md");
    [Fact] void should_cite_the_profile_on_github() => _pages.Single().Url.Value.ShouldEqual("https://github.com/Cratis/.github/blob/main/profile/README.md");
    [Fact] void should_stay_anonymous() => _authorized.ShouldBeFalse();
}
