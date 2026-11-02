// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_markup_cannot_be_classified : Specification
{
    readonly string[] _inputs =
    [
        "[outer [inner](https://elsewhere.invalid)](https://elsewhere.invalid)",
        "[Guide](https://elsewhere.invalid/a(b))",
        "[Guide](https://elsewhere.invalid/a`b)",
        "[Guide](https://cratis.io/%60%29/)",
        "[Guide](https&#58;//elsewhere.invalid)",
        "[Guide](https\\://elsewhere.invalid)",
        "\\[Guide](https://elsewhere.invalid)",
        "[Guide](//elsewhere.invalid)",
        "<a href=\"https://elsewhere.invalid/`>Guide",
        "``` ```\n@someone https://elsewhere.invalid",
        "ht\u200Btps://elsewhere.invalid HTTPS://ELSEWHERE.INVALID https\uFF1A//elsewhere\u3002invalid",
        "discord.gg/abc and www.elsewhere.invalid"
    ];
    string[] _results = [];

    void Because() => _results = _inputs.Select(input => PublishedMarkdown.Neutralize(input, new())).ToArray();

    [Fact] void should_not_return_unclassified_markup() => _results.Where((result, index) => result == _inputs[index]).Count().ShouldEqual(0);
    [Fact] void should_leave_no_active_destination() => _results.All(result => !result.Contains("elsewhere.invalid", StringComparison.OrdinalIgnoreCase) && !result.Contains("://elsewhere", StringComparison.OrdinalIgnoreCase) && !result.Contains("discord.gg", StringComparison.Ordinal) && !result.Contains("@someone", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_be_idempotent() => _results.All(result => PublishedMarkdown.Neutralize(result, new()) == result).ShouldBeTrue();
}
