// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_rows_carry_categories_and_products : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose(
        [
        new("discord", [], true, null, "docs-gap", "chronicle"),
        new("discord", [], true, null, "docs-gap", "chronicle"),
        new("discord", [], false, null, "docs-gap", "Arc"),
        new("discord", [], false, null, "missing-capability", "chronicle"),
        new("discord", [], false, null, "likely-bug", "arc"),
        new("discord", [], true, null, "out-of-scope", "arc")],
        default,
        default);
    [Fact] void should_group_and_sort_products() => _text.ShouldContain("**Docs gaps** — chronicle 2 · arc 1");
    [Fact] void should_separate_capabilities() => _text.ShouldContain("**Possible missing capabilities** — chronicle 1");
    [Fact] void should_separate_bugs() => _text.ShouldContain("**Likely bugs** — arc 1");
    [Fact] void should_omit_empty_feedback() => _text.ShouldNotContain("**Thumbs-down answers**");
    [Fact] void should_order_sections() => (_text.IndexOf("**Docs gaps**", StringComparison.Ordinal) < _text.IndexOf("**Possible missing capabilities**", StringComparison.Ordinal) && _text.IndexOf("**Possible missing capabilities**", StringComparison.Ordinal) < _text.IndexOf("**Likely bugs**", StringComparison.Ordinal)).ShouldBeTrue();
}
