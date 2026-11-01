// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_the_answer_contains_programming_examples : given.a_grounded_answerer
{
    const string Generated = """
        ```csharp
        var first = items[0];
        var later = items[2];
        // These are code, not citations: [1] [5] [99].
        ```
        ~~~text
        [1] [2] [99]
        ~~~
        ``An inline ` delimiter and [2] [99]`` and `items[0] + items[2]`.
        Expressions: items[0], items[2], matrix[1][2], GetItems()[2], items?[2], items![2].
        Escaped markers: \[1] \[99].
        E [5], B [2]. Invalid [0] [99].
        """;

    void Establish()
    {
        Found([.. Enumerable.Range(1, 5).Select(index => Passage(0.8) with { Page = new($"https://cratis.io/page-{index}.md") })]);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, Generated)));
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_preserve_fenced_code_byte_for_byte() => result.Text.ShouldContain(Generated[..Generated.IndexOf("``An inline", StringComparison.Ordinal)]);
    [Fact] void should_preserve_inline_code_with_single_and_multiple_backtick_delimiters() => result.Text.ShouldContain("``An inline ` delimiter and [2] [99]`` and `items[0] + items[2]`.");
    [Fact] void should_preserve_indexing_expressions_outside_code_markup() => result.Text.ShouldContain("Expressions: items[0], items[2], matrix[1][2], GetItems()[2], items?[2], items![2].");
    [Fact] void should_preserve_escaped_brackets() => result.Text.ShouldContain("Escaped markers: \\[1] \\[99].");
    [Fact] void should_still_remap_sparse_prose_citations_and_strip_invalid_prose_markers() => result.Text.ShouldContain("E [2], B [1]. Invalid  .");
    [Fact] void should_select_only_pages_referenced_in_prose() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/page-2.md", "https://cratis.io/page-5.md"]);
}
