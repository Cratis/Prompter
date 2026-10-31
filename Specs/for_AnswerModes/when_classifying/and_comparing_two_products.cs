// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerModes.when_classifying;

public class and_comparing_two_products : Specification
{
    [Fact] void should_be_recommendation() => AnswerModes.For("Chronicle vs Arc").ShouldEqual(AnswerMode.Recommendation);
}
