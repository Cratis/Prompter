// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_from_prompter : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For("An earlier Prompter answer", true, false, 4000);
    [Fact] void should_prevent_an_answer_loop() => _result.ShouldEqual(MessageSelectionVerdict.FromBot);
}
