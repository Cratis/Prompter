// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_ReleaseNotes.when_rendering;

public class and_chunks_follow_explicit_metadata : Specification
{
    readonly PageUrl _url = new("https://github.com/Cratis/Chronicle/releases/tag/v1.2.3");
    ReleaseNote _note = null!;
    Chunk _first = null!;
    void Establish() => _note = new("v1.2.3", null, "Changes.", false, false, null, _url.Value);
    void Because() => _first = MarkdownChunker.Chunk(_url, ReleaseNotes.Markdown("Chronicle", _note)).Single();

    [Fact] void should_reproduce_the_same_hash() => MarkdownChunker.Chunk(_url, ReleaseNotes.Markdown("Chronicle", _note)).Single().Hash.ShouldEqual(_first.Hash);
    [Fact] void should_change_the_hash_for_new_body() => MarkdownChunker.Chunk(_url, ReleaseNotes.Markdown("Chronicle", _note with { Body = "New changes." })).Single().Hash.ShouldNotEqual(_first.Hash);
    [Fact] void should_change_the_hash_for_new_version() => MarkdownChunker.Chunk(_url, ReleaseNotes.Markdown("Chronicle", _note with { TagName = "v1.2.4" })).Single().Hash.ShouldNotEqual(_first.Hash);
    [Fact] void should_keep_unknown_dates_unknown() => new DocsPage(_url, ReleaseNotes.Markdown("Chronicle", _note)).Metadata.PageLastModified.ShouldBeNull();
}
