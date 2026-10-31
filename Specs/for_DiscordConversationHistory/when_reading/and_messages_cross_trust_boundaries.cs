// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NetCord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DiscordConversationHistory.when_reading;

public class and_messages_cross_trust_boundaries : given.a_discord_history
{
    static object Message(DateTimeOffset time, string content, (ulong Id, bool IsBot)? author = null, (ulong Channel, int Type)? scope = null) => new
    {
        id = Snowflake(time).ToString(System.Globalization.CultureInfo.InvariantCulture),
        channel_id = (scope?.Channel ?? 2).ToString(System.Globalization.CultureInfo.InvariantCulture),
        author = new { id = (author?.Id ?? 7).ToString(System.Globalization.CultureInfo.InvariantCulture), username = "synthetic", bot = author?.IsBot ?? false },
        content,
        type = scope?.Type ?? 0,
        mentions = Array.Empty<object>(),
        mention_roles = Array.Empty<string>(),
        attachments = Array.Empty<object>(),
        embeds = Array.Empty<object>()
    };

    [Theory]
    [InlineData(ChannelType.TextGuildChannel)]
    [InlineData(ChannelType.PublicGuildThread)]
    async Task should_keep_only_same_scope_recent_humans_and_own_bot_answers(ChannelType type)
    {
        Configure(type);
        messages = JsonSerializer.Serialize(new[]
        {
            Message(Now.AddMinutes(-1), "recent"),
            Message(Now.AddMinutes(-2), "own answer", author: (42, true)),
            Message(Now.AddMinutes(-3), "own-id human", author: (42, false)),
            Message(Now.AddMinutes(-15), "boundary"),
            Message(Now.AddMinutes(-15).AddMilliseconds(-1), "expired"),
            Message(Now.AddMinutes(-4), "sibling", scope: (3, 0)),
            Message(Now.AddMinutes(-5), "other bot", author: (7, true)),
            Message(Now.AddMinutes(-6), "system", scope: (2, 7)),
            Message(Now, "trigger"),
            Message(Now.AddMinutes(1), "future")
        });
        using var cancellation = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource();
        await using var lease = new WorkLease(Substitute.For<ISpendLedger>(), Guid.NewGuid(), new(1, 1), cancellation, deadline, () => { });
        lease.Activate();
        var reader = new ConversationContextReader(history, new ManualClock(Now));

        var result = await reader.For("current", location, TestContext.Current.CancellationToken);

        result.Conversation.IsDegraded.ShouldBeFalse();
        result.Conversation.Messages.Select(message => message.Content).ShouldEqual(["boundary", "own-id human", "own answer", "recent"]);
        result.Conversation.Messages.Select(message => message.Role).ShouldEqual(["user", "user", "assistant", "user"]);
        requests.Count.ShouldEqual(2);
        requests[1].Contains("/channels/2/messages", StringComparison.Ordinal).ShouldBeTrue();
        requests[1].Contains($"before={location.MessageId}", StringComparison.Ordinal).ShouldBeTrue();
        requests[1].Contains("limit=10", StringComparison.Ordinal).ShouldBeTrue();
    }
}
