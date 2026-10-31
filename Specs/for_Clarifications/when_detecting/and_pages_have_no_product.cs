// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Clarifications.when_detecting;

public class and_pages_have_no_product : Specification
{
    Clarification? _result;

    void Because() => _result = Clarifications.Detect(new("How do I handle commands?"), [new(new("https://example.test/a"), "T", "H", "c", 0.01, 0.8), new(new("https://example.test/b"), "T", "H", "c", 0.01, 0.79)], "https://cratis.io", 0.03);

    [Fact] void should_not_clarify() => _result.ShouldBeNull();
}
