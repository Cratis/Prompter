// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_MessageSelection.when_judging;

public class and_the_message_is_ordinary : Specification
{
    MessageSelectionVerdict _result;
    void Because() => _result = MessageSelection.For("How does Arc work?", false, false, 4000);
    [Fact] void should_accept() => _result.ShouldEqual(MessageSelectionVerdict.Accept);
    [Fact] void should_accept_the_exact_ceiling() => MessageSelection.For(new string('a', 4000), false, false, 4000).ShouldEqual(MessageSelectionVerdict.Accept);
}
