// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerModes.when_classifying;

public class and_asking_which_tool_to_use : Specification
{
    [Fact] void should_be_recommendation() => AnswerModes.For("Which package should I use for read models?").ShouldEqual(AnswerMode.Recommendation);
}
