// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_OverviewQuestions.when_classifying;

public class and_asking_a_broad_overview_question : Specification
{
    [Theory]
    [InlineData("What is Cratis good at?")]
    [InlineData("Why use Cratis?")]
    [InlineData("What are Cratis's strengths?")]
    [InlineData("what is Cratis, in a nutshell?")]
    [InlineData("What is Cratis?")]
    [InlineData("Why should I choose Cratis?")]
    [InlineData("What are the benefits of Cratis?")]
    [InlineData("Is Cratis good for IoT?")]
    public void should_route_to_the_overview(string question) => OverviewQuestions.Matches(question).ShouldBeTrue();
}
