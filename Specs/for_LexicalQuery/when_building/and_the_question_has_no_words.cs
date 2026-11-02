// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_LexicalQuery.when_building;

public class and_the_question_has_no_words : Specification
{
    [Fact] void should_be_empty() => LexicalQuery.Lenient("?! -- ").ShouldEqual(string.Empty);
    [Fact] void should_be_empty_for_only_the_word_or() => LexicalQuery.Lenient("or").ShouldEqual(string.Empty);
    [Fact] void should_be_empty_for_only_the_brand_name() => LexicalQuery.Lenient("Cratis?").ShouldEqual(string.Empty);
}
