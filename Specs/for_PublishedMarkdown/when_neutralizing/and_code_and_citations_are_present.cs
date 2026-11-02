// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_code_and_citations_are_present : Specification
{
    const string Markdown = "[Docs](https://cratis.io/arc/) and <https://github.com/Cratis/Arc> [1].\n\n`https://cratis.io/arc/`\n\n```cs\n// https://cratis.io/chronicle/\n```\n~~~text\n[Docs](https://cratis.io/arc/)\n~~~\nSee www.cratis.io/arc and cratis.io/arc.\n";
    string _result = string.Empty;

    void Because() => _result = PublishedMarkdown.Neutralize(Markdown, new());

    [Fact] void should_preserve_allowed_citations_in_code_and_prose() => _result.ShouldEqual(Markdown);
    [Fact] void should_be_idempotent() => PublishedMarkdown.Neutralize(_result, new()).ShouldEqual(_result);
}
