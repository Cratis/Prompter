// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_suggestions_are_malformed : Specification
{
    GeneratedAnswer _result = null!;

    void Because() => _result = AnswerGeneration.Parse("""{"text":"t","category":"answered","product":"Arc","suggestions":["One?",5,null,"Two?"]}""")!;

    [Fact] void should_drop_the_non_strings() => string.Join('|', _result.Suggestions).ShouldEqual("One?|Two?");
}
