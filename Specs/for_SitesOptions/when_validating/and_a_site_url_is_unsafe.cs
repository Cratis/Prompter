// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_SitesOptions.when_validating;

public class and_a_site_url_is_unsafe : Specification
{
    [Theory]
    [InlineData("http://cratis.no")]
    [InlineData("https://user@cratis.no")]
    [InlineData("https://cratis.no:8443")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://cratis.no/about")]
    [InlineData("https://cratis.no/?q=x")]
    [InlineData("https://cratis.no/#x")]
    public void should_reject_unsafe_roots(string root) => new SiteOptions { BaseUrl = root }.IsValid.ShouldBeFalse();

    [Fact] public void should_accept_site_defaults() => new SitesOptions().IsValid.ShouldBeTrue();
    [Fact] public void should_accept_readme_defaults() => new ReadmesOptions().IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void should_reject_invalid_page_caps(int cap) => new SiteOptions { MaxPages = cap }.IsValid.ShouldBeFalse();

    [Fact] public void should_require_rooted_paths() => new SiteOptions { Pages = ["relative"] }.IsValid.ShouldBeFalse();
    [Fact] public void should_bound_response_bytes() => new SitesOptions { MaxPageBytes = 1 }.IsValid.ShouldBeFalse();
    [Fact] public void should_require_positive_readme_bounds() => new ReadmesOptions { MaxRepositories = 0 }.IsValid.ShouldBeFalse();
}
