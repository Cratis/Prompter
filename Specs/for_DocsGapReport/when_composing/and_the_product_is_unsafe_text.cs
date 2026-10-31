// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_the_product_is_unsafe_text : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose(
        [
        new("discord", [], true, null, "docs-gap", "@everyone\n**hello**"),
        new("discord", [], true, null, "docs-gap", new string('a', 41)),
        new("discord", ["not a url"], true, null, "docs-gap", null)],
        default,
        default);
    [Fact] void should_replace_unsafe_products() => _text.ShouldContain("**Docs gaps** — unknown 3");
    [Fact] void should_not_emit_mentions() => _text.ShouldNotContain("@");
    [Fact] void should_not_emit_raw_markdown() => _text.ShouldNotContain("hello");
}
