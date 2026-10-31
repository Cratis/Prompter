// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordConversationHistory.when_reading_a_longer_window;

public class and_a_twenty_message_limit_is_requested : given.a_discord_history
{
    IReadOnlyList<HistoryMessage> _result = [];
    void Establish()
    {
        messages = "[" + string.Join(',', Enumerable.Range(1, 30).Select(offset =>
            $$"""{"id":"{{Snowflake(Now.AddMinutes(-offset))}}","channel_id":"2","type":0,"content":"message","author":{"id":"4","username":"person"},"attachments":[],"embeds":[],"mentions":[],"mention_roles":[]}""")) + "]";
        Configure();
    }
    async Task Because() => _result = await history.Before(location, 20, CancellationToken.None);
    [Fact] void should_stop_after_twenty() => _result.Count.ShouldEqual(20);
    [Fact] void should_request_the_explicit_batch_size() => requests.Any(path => path.Contains("limit=20", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_request_only_the_selected_scope() => requests.Where(path => path.Contains("/messages", StringComparison.Ordinal)).All(path => path.Contains("/channels/2/messages", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_request_before_the_selected_message() => requests.Any(path => path.Contains($"before={location.MessageId}", StringComparison.Ordinal)).ShouldBeTrue();
}
