// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_QuestionExcerpt.when_excerpting;

public class and_text_is_bounded : Specification
{
    [Fact] void should_collapse_whitespace() => QuestionExcerpt.For("  short\n question  ", 90).ShouldEqual("short question");
    [Fact] void should_truncate_with_an_ellipsis() => QuestionExcerpt.For("long question", 5).ShouldEqual("long…");
    [Fact] void should_remove_mentions() => QuestionExcerpt.For("<@123> hello @everyone", 90).ShouldEqual("hello");
    [Fact] void should_return_empty_for_blank_text() => QuestionExcerpt.For(" \n ", 90).ShouldEqual(string.Empty);
}
