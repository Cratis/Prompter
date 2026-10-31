// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_the_category_is_unrecognized : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose(
        [
        new("discord", [], false, null, "@everyone", "arc"),
        new("discord", [], true, null, "unrecognized", "chronicle")],
        default,
        default);
    [Fact] void should_treat_unknown_categories_as_null() => _text.ShouldContain("**Docs gaps** — chronicle 1");
    [Fact] void should_not_print_raw_categories() => _text.ShouldNotContain("@everyone");
    [Fact] void should_flag_the_uncategorized_refusal() => _text.ShouldContain("_1 older refusals");
}
