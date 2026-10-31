// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;

namespace Cratis.Prompter.Specs.for_DiscordConversationHistory.when_authorizing;

public class and_visibility_is_restricted : given.a_discord_history
{
    [Theory]
    [InlineData(ChannelType.DMChannel)]
    [InlineData(ChannelType.GroupDMChannel)]
    [InlineData(ChannelType.PrivateGuildThread)]
    [InlineData(ChannelType.CategoryChannel)]
    async Task should_deny_nonpublic_channel_types(ChannelType type)
    {
        Configure(type);
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeFalse();
        requests.Count.ShouldEqual(1);
    }

    [Theory]
    [InlineData(ChannelType.TextGuildChannel)]
    [InlineData(ChannelType.PublicGuildThread)]
    async Task should_deny_a_private_channel_or_private_thread_parent(ChannelType type)
    {
        const string denied = """[{"id":"1","type":0,"allow":"0","deny":"1024"}]""";
        Configure(type, overwrites: denied, scope: new() { ParentOverwrites = denied });
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(ChannelType.TextGuildChannel, 2, 2)]
    [InlineData(ChannelType.PublicGuildThread, 2, 2)]
    [InlineData(ChannelType.TextGuildChannel, 1, 99)]
    [InlineData(ChannelType.PublicGuildThread, 1, 99)]
    async Task should_deny_a_different_guild_or_channel(ChannelType type, ulong returnedGuildId, ulong returnedChannelId)
    {
        Configure(type, scope: new() { GuildId = returnedGuildId, ChannelId = returnedChannelId });
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    async Task should_not_fetch_without_a_cached_guild()
    {
        Configure(scope: new() { IncludeGuild = false });
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeFalse();
        requests.ShouldBeEmpty();
    }

    [Fact]
    async Task should_not_fetch_history_for_a_direct_message_location()
    {
        Configure(ChannelType.DMChannel);
        (await history.IsPublic(location with { GuildId = 0 }, CancellationToken.None)).ShouldBeFalse();
        requests.ShouldBeEmpty();
    }

    [Fact]
    async Task should_inherit_role_restrictions_from_a_public_threads_parent()
    {
        Configure(ChannelType.PublicGuildThread, scope: new() { ParentOverwrites = """[{"id":"9","type":0,"allow":"0","deny":"1024"}]""" });
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeFalse();
    }

    [Theory]
    [InlineData((Permissions)0, "[]", false)]
    [InlineData((Permissions)0, "[{\"id\":\"1\",\"type\":0,\"allow\":\"1024\",\"deny\":\"0\"}]", true)]
    [InlineData((Permissions)0, "[{\"id\":\"9\",\"type\":0,\"allow\":\"1024\",\"deny\":\"0\"}]", false)]
    [InlineData(Permissions.ViewChannel, "[{\"id\":\"9\",\"type\":0,\"allow\":\"0\",\"deny\":\"1024\"}]", false)]
    [InlineData(Permissions.ViewChannel, "[{\"id\":\"9\",\"type\":1,\"allow\":\"0\",\"deny\":\"1024\"}]", false)]
    [InlineData(Permissions.ViewChannel, "[{\"id\":\"1\",\"type\":0,\"allow\":\"0\",\"deny\":\"1024\"},{\"id\":\"9\",\"type\":0,\"allow\":\"1024\",\"deny\":\"0\"}]", false)]
    [InlineData(Permissions.Administrator, "[{\"id\":\"1\",\"type\":0,\"allow\":\"0\",\"deny\":\"1024\"}]", true)]
    async Task should_require_visibility_for_everyone_not_just_an_allowed_role(Permissions permissions, string overwrites, bool expected)
    {
        Configure(permissions: permissions, overwrites: overwrites);
        (await history.IsPublic(location, CancellationToken.None)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData(ChannelType.TextGuildChannel)]
    [InlineData(ChannelType.PublicGuildThread)]
    [InlineData(ChannelType.AnnouncementGuildThread)]
    async Task should_allow_a_public_scope_without_reading_parent_messages(ChannelType type)
    {
        Configure(type);
        (await history.IsPublic(location, CancellationToken.None)).ShouldBeTrue();
        requests.Count.ShouldEqual(1);
        requests[0].Contains("/channels/2", StringComparison.Ordinal).ShouldBeTrue();
    }
}
