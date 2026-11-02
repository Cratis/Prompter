// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_the_text_is_already_neutral : Specification
{
    readonly string[] _inputs =
    [
        "[Docs](https://cratis.io/arc/) [x](https://evil.invalid) @everyone <@1> www.evil.invalid discord.gg/abc https://cratis.io/a/https://evil.invalid",
        "```\nhttps://evil.invalid @here </cmd:1>\n```\n<https://cratis.io/arc/>",
        "discord.gg/abc\n\n" + new string('a', PublishedMarkdown.MaximumLength + 1)
    ];
    string[] _results = [];

    void Because() => _results = _inputs.Select(input => PublishedMarkdown.Neutralize(input, new())).ToArray();

    [Fact] void should_not_change_on_another_pass() => _results.All(result => PublishedMarkdown.Neutralize(result, new()) == result).ShouldBeTrue();
}
