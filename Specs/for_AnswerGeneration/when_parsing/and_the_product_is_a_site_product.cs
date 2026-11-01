// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_product_is_a_site_product : Specification
{
    [Theory]
    [InlineData("Specifications", 6)]
    [InlineData("Stage", 7)]
    [InlineData("Screenplay", 8)]
    [InlineData("Prologue", 9)]
    [InlineData("Studio", 10)]
    [InlineData("Direct", 11)]
    [InlineData("Assurance", 12)]
    [InlineData("studio", 0)]
    public void should_map_the_product_to_its_stable_code(string wire, int code) =>
        ((int)AnswerGeneration.Parse($$"""{"text":"Answer","category":"answered","product":"{{wire}}"}""")!.Product).ShouldEqual(code);
}
