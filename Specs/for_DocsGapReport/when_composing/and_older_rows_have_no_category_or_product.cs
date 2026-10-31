// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_older_rows_have_no_category_or_product : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose(
        [
        new("discord", ["https://cratis.io/chronicle/events.md"], true, null, null, null),
        new("discord", [], true, null, null, null),
        new("discord", ["https://cratis.io/arc.md/"], true, null, null, "unknown"),
        new("discord", [], false, null, null, null)],
        default,
        default);
    [Fact] void should_derive_and_sort_products() => _text.ShouldContain("**Docs gaps** — arc 1 · chronicle 1 · unknown 1");
    [Fact] void should_explain_legacy_refusals() => _text.ShouldContain("_3 older refusals had no category and are counted as docs gaps._");
}
