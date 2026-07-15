// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_AskArguments.when_parsing;

public class and_the_verbose_flag_follows_the_question : Specification
{
    AskArguments _result = null!;

    void Because() => _result = AskArguments.Parse(["How", "do", "I", "append", "events", "--verbose"]);

    [Fact] void should_be_verbose() => _result.Verbose.ShouldBeTrue();
    [Fact] void should_strip_the_flag_from_the_question() => _result.Question.ShouldEqual("How do I append events");
}
