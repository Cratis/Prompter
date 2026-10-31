// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_many_products_exceed_the_section_limit : Specification
{
    static readonly string[] _categories = ["docs-gap", "missing-capability", "likely-bug"];
    string _short = null!;
    string _long = null!;
    void Because()
    {
        _short = DocsGapReport.Compose(Enumerable.Range(0, 12).Select(index => new DocsGapRow("discord", [], false, null, "docs-gap", $"p{index:D2}")).ToArray(), default, default);
        var rows = _categories.SelectMany(category => Enumerable.Range(0, 100).Select(index => new DocsGapRow("discord", [], true, "down", category, $"p{index:D2}-{new string('a', 36)}"))).ToArray();
        _long = DocsGapReport.Compose(rows, default, default);
    }
    [Fact] void should_sort_equal_counts_by_name() => _short.ShouldContain("p00 1 · p01 1");
    [Fact] void should_cap_at_ten_products() => _short.ShouldContain("p09 1 · …and 2 more");
    [Fact] void should_omit_the_eleventh_product() => _short.ShouldNotContain("p10");
    [Fact] void should_remain_inside_the_discord_limit() => (_long.Length <= 2000).ShouldBeTrue();
    [Fact] void should_mark_truncation_in_each_section() => _long.Split("…and").Length.ShouldEqual(5);
}
