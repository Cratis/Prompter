// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_TruncatedText.when_salvaging;

public class and_finding_citations_times_out : Specification
{
    static readonly byte[] _cut = Encoding.UTF8.GetBytes("{\"text\":\"- **Chronicle**: event store [1]\\n- **Arc**: CQRS [1]\\n- **Comp");

    string? _result = "unset";

    void Because() => _result = TruncatedText.Salvage(_cut, 7, _ => throw new RegexMatchTimeoutException());

    [Fact] void should_skip_salvage_instead_of_failing() => _result.ShouldBeNull();
    [Fact] void should_still_salvage_when_the_match_completes() => TruncatedText.Salvage(_cut, 7).ShouldNotBeNull();
}
