// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SourceHosts.when_checking_citations;

public class and_hosts_vary : Specification
{
    [Theory]
    [InlineData("https://cratis.io/x.md", true)]
    [InlineData("https://www.cratis.io/x.md", true)]
    [InlineData("https://cratis.no/about/", true)]
    [InlineData("https://cratis.studio/vision.html", true)]
    [InlineData("https://cratis.direct/", true)]
    [InlineData("https://github.com/Cratis/Arc/blob/main/README.md", true)]
    [InlineData("https://evil.invalid/", false)]
    [InlineData("https://cratis.no.evil.invalid/", false)]
    [InlineData("https://github.com.evil/Cratis/x", false)]
    [InlineData("https://github.com/Other/x", false)]
    [InlineData("http://cratis.no/", false)]
    [InlineData("https://user@cratis.no/", false)]
    [InlineData("https://cratis.no:8443/", false)]
    [InlineData("https://127.0.0.1/", false)]
    public void should_require_an_exact_safe_host(string url, bool expected) =>
        SourceHosts.IsCitable(new(url), new()).ShouldEqual(expected);

    [Fact]
    public void should_follow_configured_hosts_even_if_disabled()
    {
        var options = new PrompterOptions();
        options.Ingestion.Sites.Company.BaseUrl = "https://company.cratis.no";
        options.Ingestion.Sites.Company.Enabled = false;
        SourceHosts.IsCitable(new("https://company.cratis.no/about/"), options).ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://cratis.no/x", true)]
    [InlineData("http://cratis.no/x", false)]
    [InlineData("https://cratis.no.evil.invalid/x", false)]
    [InlineData("https://user@cratis.no/", false)]
    [InlineData("https://cratis.no:8443/", false)]
    public void should_bound_fetches_to_one_host(string url, bool expected) =>
        SourceHosts.IsFetchable(new(url), "cratis.no").ShouldEqual(expected);
}
