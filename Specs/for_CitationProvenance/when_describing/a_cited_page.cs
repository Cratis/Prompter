// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_CitationProvenance.when_describing;

public class a_cited_page : Specification
{
    static readonly PageUrl _page = new("https://cratis.io/a");

    [Fact] void should_show_the_utc_index_date_and_version() => new CitationProvenance(_page, new DateTimeOffset(2026, 10, 30, 23, 30, 0, TimeSpan.FromHours(-5)), null, "16.0.0").Note.ShouldEqual("indexed 2026-10-31 · v16.0.0");
    [Fact] void should_say_when_the_index_date_is_unknown() => new CitationProvenance(_page, null, null, null).Note.ShouldEqual("index date unknown");
    [Fact] void should_omit_an_unavailable_version() => new CitationProvenance(_page, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), null, null).Note.ShouldEqual("indexed 2026-01-02");
    [Fact] void should_not_render_the_source_modified_date() => new CitationProvenance(_page, null, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), null).Note.ShouldNotContain("2026");
    [Fact] void should_make_no_freshness_promise() => new CitationProvenance(_page, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), null, "1.0.0").Note.ShouldNotContain("latest");
}
