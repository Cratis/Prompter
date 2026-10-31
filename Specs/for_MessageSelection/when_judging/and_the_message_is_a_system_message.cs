// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_a_system_message : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For(string.Empty, true, true, 0);
    [Fact] void should_reject_before_bot_and_text_checks() => _result.ShouldEqual(MessageSelectionVerdict.System);
}
