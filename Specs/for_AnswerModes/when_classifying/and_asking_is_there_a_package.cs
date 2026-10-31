// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerModes.when_classifying;

public class and_asking_is_there_a_package : Specification
{
    [Fact] void should_be_recommendation() => AnswerModes.For("Is there a package for MongoDB?").ShouldEqual(AnswerMode.Recommendation);
}
