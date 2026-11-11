// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_ReadmeMarkdown.when_cleaning;

public class and_fence_markers_do_not_match : Specification
{
    string _clean = null!;

    void Because() => _clean = ReadmeMarkdown.Clean("# Title\n\n~~~html\n```\n<b>code</b>\n~~~\n<b>text</b>\n\n\n", "Title", string.Empty, 1000);

    [Fact] void should_keep_tags_until_the_matching_fence_closes() => _clean.ShouldContain("~~~html\n```\n<b>code</b>\n~~~\ntext");
    [Fact] void should_keep_the_existing_title() => _clean.EndsWith("# Title\n\n~~~html\n```\n<b>code</b>\n~~~\ntext", StringComparison.Ordinal).ShouldBeTrue();
}
