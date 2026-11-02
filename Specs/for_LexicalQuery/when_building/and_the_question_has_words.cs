// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_LexicalQuery.when_building;

public class and_the_question_has_words : Specification
{
    [Fact] void should_join_the_words_with_or() => LexicalQuery.Lenient("What are Cratis's strengths?").ShouldEqual("What or are or Cratis or s or strengths");
    [Fact] void should_drop_the_word_or() => LexicalQuery.Lenient("events OR commands").ShouldEqual("events or commands");
    [Fact] void should_remove_duplicates_ignoring_case() => LexicalQuery.Lenient("Arc arc ARC Chronicle").ShouldEqual("Arc or Chronicle");
    [Fact] void should_drop_operators_and_quotes() => LexicalQuery.Lenient("-\"a\"").ShouldEqual("a");
    [Fact] void should_keep_at_most_32_terms() => LexicalQuery.Lenient(string.Join(' ', Enumerable.Range(0, 40).Select(number => $"w{number}"))).Split(" or ").Length.ShouldEqual(32);
}
