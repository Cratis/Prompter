// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_suggestions_are_missing : Specification
{
    GeneratedAnswer _result = null!;

    void Because() => _result = AnswerGeneration.Parse("""{"text":"t","category":"answered","product":"Arc"}""")!;

    [Fact] void should_be_empty() => _result.Suggestions.ShouldBeEmpty();
}
