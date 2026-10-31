// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_an_answered_row_got_a_thumbs_down : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose([new("discord", [], false, "down", "answered", "chronicle")], default, default);
    [Fact] void should_count_feedback_independently() => _text.ShouldContain("**Thumbs-down answers** — chronicle 1");
    [Fact] void should_not_call_it_a_gap() => _text.ShouldNotContain("**Docs gaps**");
}
