// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_parsing_sitemap;

public class and_exclusions_are_customized : Specification
{
    const string Sitemap =
        """
        <urlset>
            <url><loc>https://cratis.io/chronicle/events/</loc></url>
            <url><loc>https://cratis.io/internal/notes/</loc></url>
            <url><loc>https://cratis.io/api-reference/chronicle/</loc></url>
        </urlset>
        """;

    static readonly string[] _exclusions = ["internal"];

    PageUrl[] _urls = null!;

    void Because() => _urls = [.. DocsSite.ParsePageUrls(Sitemap, _exclusions)];

    [Fact] void should_exclude_the_custom_segment() => _urls.Any(url => url.Value.Contains("internal")).ShouldBeFalse();
    [Fact] void should_keep_segments_not_in_the_custom_list() => _urls.ShouldContain(new PageUrl("https://cratis.io/api-reference/chronicle.md"));
    [Fact] void should_keep_documentation_pages() => _urls.ShouldContain(new PageUrl("https://cratis.io/chronicle/events.md"));
}
