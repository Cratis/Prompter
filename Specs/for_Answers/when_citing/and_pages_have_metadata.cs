// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_pages_have_metadata : given.a_grounded_answerer
{
    static readonly DateTimeOffset _indexed = new(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);

    static Passage Stamp(Passage passage, DateTimeOffset indexed, string version) => passage with { IndexedAt = indexed, ProductVersion = version };

    void Establish()
    {
        var first = Passage(0.8) with { Page = new("https://cratis.io/a.md") };
        var second = Passage(0.7) with { Page = new("https://cratis.io/a.md") };
        var other = Passage(0.6) with { Page = new("https://cratis.io/b.md") };
        var disputed = Passage(0.5) with { Page = new("https://cratis.io/c.md") };
        var disputing = Passage(0.5) with { Page = new("https://cratis.io/c.md") };
        Found(
            Stamp(first, _indexed, "1.0.0"),
            Stamp(second, _indexed, "1.0.0"),
            other,
            Stamp(disputed, _indexed, "1.0.0"),
            Stamp(disputing, _indexed, "2.0.0"));
    }

    async Task Because() => result = await answers.For(new("Q?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_keep_the_citations_unchanged() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/a.md", "https://cratis.io/b.md", "https://cratis.io/c.md"]);
    [Fact] void should_follow_citation_order() => result.CitationProvenance.Select(provenance => provenance.Page.Value).ShouldEqual(["https://cratis.io/a.md", "https://cratis.io/b.md", "https://cratis.io/c.md"]);
    [Fact] void should_take_agreed_metadata_from_retrieval() => result.CitationProvenance[0].Note.ShouldEqual("indexed 2026-10-30 · v1.0.0");
    [Fact] void should_mark_a_page_without_metadata_unknown() => result.CitationProvenance[1].Note.ShouldEqual("index date unknown");
    [Fact] void should_omit_disputed_values() => result.CitationProvenance[2].ProductVersion.ShouldBeNull();
}
