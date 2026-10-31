// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_from_a_bot : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For(string.Empty, true, false, 4000);
    [Fact] void should_reject_before_judging_text() => _result.ShouldEqual(MessageSelectionVerdict.FromBot);
}
