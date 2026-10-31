// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_empty : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For(" \n\t", false, false, 4000);
    [Fact] void should_reject() => _result.ShouldEqual(MessageSelectionVerdict.Empty);
    [Fact] void should_reject_empty_text() => MessageSelection.For(string.Empty, false, false, 4000).ShouldEqual(MessageSelectionVerdict.Empty);
    [Fact] void should_judge_whitespace_before_length() => MessageSelection.For("   ", false, false, 1).ShouldEqual(MessageSelectionVerdict.Empty);
}
