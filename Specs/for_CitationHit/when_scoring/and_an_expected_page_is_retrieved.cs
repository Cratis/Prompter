// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_CitationHit.when_scoring;

public class and_an_expected_page_is_retrieved : Specification
{
    bool _result;

    void Because() => _result = CitationHit.IsHit(
        ["https://cratis.io/chronicle/events/appending/"],
        ["https://cratis.io/chronicle/events/appending.md", "https://cratis.io/chronicle/reactors.md"]);

    [Fact] void should_be_a_hit() => _result.ShouldBeTrue();
}
