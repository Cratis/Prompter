// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_content_contains_sensitive_text : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>(
    [Human(90, "<@123> <@!456> <@&789> <#999> @everyone @here @example Bearer " + new string('x', 30))]));

    async Task Because() => result = await Read("<@123> explain api_key=" + new string('x', 24));

    [Fact] void should_strip_mentions_and_scrub_history() => result.Conversation.Messages.Single().Content.ShouldEqual("[REDACTED_SECRET]");
    [Fact] void should_scrub_the_current_request_too() => result.Value.ShouldEqual("explain api_key=[REDACTED_SECRET]");
}
