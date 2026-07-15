// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_CitationHit.when_scoring;

public class and_the_retrieved_page_keeps_its_markdown_suffix : Specification
{
    bool _result;

    void Because() => _result = CitationHit.IsHit(
        ["https://cratis.io/fundamentals/csharp/concepts/"],
        ["https://cratis.io/fundamentals/csharp/concepts.md"]);

    [Fact] void should_normalize_the_md_suffix_and_hit() => _result.ShouldBeTrue();
}
