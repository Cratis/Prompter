// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_AskArguments.when_parsing;

public class and_no_flag_is_present : Specification
{
    AskArguments _result = null!;

    void Because() => _result = AskArguments.Parse(["what", "is", "Chronicle"]);

    [Fact] void should_not_be_verbose() => _result.Verbose.ShouldBeFalse();
    [Fact] void should_keep_the_whole_question() => _result.Question.ShouldEqual("what is Chronicle");
}
