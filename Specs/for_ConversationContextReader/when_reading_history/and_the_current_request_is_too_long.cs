// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_current_request_is_too_long : given.a_history_reader
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => Read(new string('q', 4001)));

    [Fact] void should_reject_instead_of_truncating_current() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_fetch_history() => history.ReceivedCalls().ShouldBeEmpty();
}
