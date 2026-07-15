// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_CitationHit.when_scoring;

public class and_no_pages_are_expected : Specification
{
    bool _result;

    void Because() => _result = CitationHit.IsHit([], ["https://cratis.io/chronicle/events/appending.md"]);

    [Fact] void should_not_be_a_hit() => _result.ShouldBeFalse();
}
