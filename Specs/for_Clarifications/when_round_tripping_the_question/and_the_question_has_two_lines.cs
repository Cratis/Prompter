// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Clarifications.when_round_tripping_the_question;

public class and_the_question_has_two_lines : Specification
{
    string _text = null!;

    void Because() => _text = Clarifications.Text(new([InteractionProduct.Chronicle, InteractionProduct.Arc, InteractionProduct.Cli]), "line one\nline two");

    [Fact] void should_list_the_products() => _text.StartsWith("This could be about Chronicle, Arc or CLI. Which product do you mean?", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_read_the_question_back() => Clarifications.QuestionFrom(_text).ShouldEqual("line one\nline two");
    [Fact] void should_read_nothing_from_unquoted_content() => Clarifications.QuestionFrom("no quote").ShouldBeNull();
}
