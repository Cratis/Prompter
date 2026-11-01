// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_markdown_mirrors_are_enabled : given.a_site
{
    void Establish()
    {
        _site.Sitemap = string.Empty;
        _site.Pages = ["/vision.html", "/features/"];
        _site.MarkdownMirrors = true;
        _responses["https://cratis.no/vision.md"] = (HttpStatusCode.OK, "text/markdown", "# Vision\n\nMarkdown mirror");
        _responses["https://cratis.no/features/"] = (HttpStatusCode.OK, "text/html", "<main><h1>Features</h1></main>");
    }
    Task Because() => Read();
    [Fact] void should_use_markdown_and_fall_back_only_on_missing_mirrors() => _requests.ShouldEqual(["https://cratis.no/vision.md", "https://cratis.no/features/index.md", "https://cratis.no/features/"]);
    [Fact] void should_never_cite_a_mirror() => _pages.Select(page => page.Url.Value).ShouldEqual(["https://cratis.no/vision.html", "https://cratis.no/features/"]);
    [Fact] void should_retain_markdown() => _pages[0].Markdown.ShouldContain("Markdown mirror");
    [Fact] void should_complete() => _error.ShouldBeNull();
}
