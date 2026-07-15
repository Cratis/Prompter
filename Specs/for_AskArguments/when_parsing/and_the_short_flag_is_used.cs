// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_AskArguments.when_parsing;

public class and_the_short_flag_is_used : Specification
{
    AskArguments _result = null!;

    void Because() => _result = AskArguments.Parse(["-v", "what", "is", "an", "aggregate"]);

    [Fact] void should_be_verbose() => _result.Verbose.ShouldBeTrue();
    [Fact] void should_strip_the_short_flag() => _result.Question.ShouldEqual("what is an aggregate");
}
