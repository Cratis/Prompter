// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerModes.when_classifying;

public class and_naming_one_product_with_or : Specification
{
    [Fact] void should_be_standard() => AnswerModes.For("Does Chronicle or something similar replay events?").ShouldEqual(AnswerMode.Standard);
}
