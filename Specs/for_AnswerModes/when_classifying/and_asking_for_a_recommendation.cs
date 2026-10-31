// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerModes.when_classifying;

public class and_asking_for_a_recommendation : Specification
{
    [Fact] void should_be_recommendation() => AnswerModes.For("Can you recommend something for events?").ShouldEqual(AnswerMode.Recommendation);
}
