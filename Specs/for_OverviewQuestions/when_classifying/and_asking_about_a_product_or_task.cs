// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_OverviewQuestions.when_classifying;

public class and_asking_about_a_product_or_task : Specification
{
    [Theory]
    [InlineData("what is Cratis Studio and is there a free trial?")]
    [InlineData("what is Cratis Components? is it just PrimeReact under the hood?")]
    [InlineData("what is Cratis Direct?")]
    [InlineData("what is Cratis Assurance?")]
    [InlineData("Why use Cratis Chronicle?")]
    [InlineData("how do I install the Cratis CLI?")]
    [InlineData("What are all cratis products and what do they do?")]
    [InlineData("what's a Concept / ConceptAs<T> in Cratis, and why would I wrap a plain string or guid in one?")]
    [InlineData("is Cratis production ready? what do I need to think about before going live?")]
    public void should_not_route_to_the_overview(string question) => OverviewQuestions.Matches(question).ShouldBeFalse();
}
