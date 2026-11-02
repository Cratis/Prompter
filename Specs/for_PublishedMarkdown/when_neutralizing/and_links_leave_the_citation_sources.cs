// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_links_leave_the_citation_sources : Specification
{
    readonly string[] _inputs =
    [
        "https://elsewhere.invalid/path.",
        "[Guide](https://elsewhere.invalid/path)",
        "![Image](https://elsewhere.invalid/image)",
        "<https://elsewhere.invalid/path>",
        "[Guide][ref]\n\n[ref]: https://elsewhere.invalid/path",
        "<a href=\"https://elsewhere.invalid/path\">Guide</a>",
        "<img src=\"https://elsewhere.invalid/image\">",
        "https://github.com/Other/Project",
        "[Guide](http://cratis.io/arc/)",
        "[Guide](javascript:alert)",
        "[Guide](https://cratis.io.evil.invalid/arc/)",
        "www.elsewhere.invalid/path"
    ];
    string[] _results = [];

    void Because() => _results = _inputs.Select(input => PublishedMarkdown.Neutralize(input, new())).ToArray();

    [Fact] void should_transform_every_nonapproved_destination() => _results.Where((result, index) => result == _inputs[index]).Count().ShouldEqual(0);
    [Fact] void should_leave_no_active_destination() => _results.Any(result => result.Contains("://elsewhere", StringComparison.Ordinal) || result.Contains("www.elsewhere", StringComparison.Ordinal) || result.Contains("://cratis.io.evil", StringComparison.Ordinal) || result.Contains("javascript:", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_be_idempotent() => _results.All(result => PublishedMarkdown.Neutralize(result, new()) == result).ShouldBeTrue();
    [Fact] void should_keep_a_safe_label() => _results[1].ShouldEqual("Guide (https[:]//elsewhere[.]invalid/path)");
    [Fact] void should_leave_trailing_sentence_punctuation_outside_the_defanged_url() => _results[0].ShouldEqual("https[:]//elsewhere[.]invalid/path.");
}
