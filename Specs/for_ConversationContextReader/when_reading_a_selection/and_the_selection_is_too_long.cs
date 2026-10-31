// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_selection_is_too_long : given.a_selection_reader
{
    Exception? _failure;
    async Task Because() => _failure = await Catch.Exception(() => Select(new string('a', 4001)));
    [Fact] void should_refuse_before_history_work() => (_failure is WorkRefused).ShouldBeTrue();
    [Fact] void should_not_read_history() => history.ReceivedCalls().ShouldBeEmpty();
}
