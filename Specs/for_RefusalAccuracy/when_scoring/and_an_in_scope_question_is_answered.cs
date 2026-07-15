// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_RefusalAccuracy.when_scoring;

public class and_an_in_scope_question_is_answered : Specification
{
    bool _result;

    void Because() => _result = RefusalAccuracy.IsCorrect(expectedRefusal: false, actualRefusal: false);

    [Fact] void should_be_correct() => _result.ShouldBeTrue();
}
