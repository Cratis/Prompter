// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_too_long : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For(new string('a', 4001), false, false, 4000);
    [Fact] void should_reject() => _result.ShouldEqual(MessageSelectionVerdict.TooLong);
    [Fact] void should_obey_a_lower_configured_ceiling() => MessageSelection.For("Arc", false, false, 2).ShouldEqual(MessageSelectionVerdict.TooLong);
}
