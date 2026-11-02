// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_code_and_citations_are_present : Specification
{
    const string Markdown = "[Docs](https://cratis.io/arc/) and <https://github.com/Cratis/Arc> [1].\n\n`@someone https://elsewhere.invalid`\n\n```cs\n// @someone https://elsewhere.invalid\n```\n~~~text\n@Cratis/team\n~~~\n";
    string _result = string.Empty;

    void Because() => _result = PublishedMarkdown.Neutralize(Markdown, new());

    [Fact] void should_preserve_examples_and_allowed_citations() => _result.ShouldEqual(Markdown);
    [Fact] void should_be_idempotent() => PublishedMarkdown.Neutralize(_result, new()).ShouldEqual(_result);
}
