// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Clarifications.when_detecting;

public class and_the_question_names_a_site_product : Specification
{
    static readonly Passage[] _passages = [new(new("https://cratis.io/chronicle/a/"), "T", "H", "c", 0.01, 0.8), new(new("https://cratis.io/arc/b/"), "T", "H", "c", 0.01, 0.79)];
    [Theory]
    [InlineData("Studio")]
    [InlineData("Screenplay")]
    [InlineData("Prologue")]
    [InlineData("Assurance")]
    [InlineData("Cratis Direct")]
    [InlineData("Cratis Stage")]
    public void should_not_clarify_an_explicit_product(string product) => Clarifications.Detect(new($"How do I use {product}?"), _passages, "https://cratis.io", 0.03).ShouldBeNull();

    [Theory]
    [InlineData("direct")]
    [InlineData("stage")]
    [InlineData("specifications")]
    [InlineData("debug in Visual Studio")]
    public void should_not_treat_common_words_as_product_names(string word) => Clarifications.Detect(new($"How do I {word}?"), _passages, "https://cratis.io", 0.03).ShouldNotBeNull();
}
