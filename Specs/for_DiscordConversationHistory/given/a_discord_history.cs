// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Prompter.Discord;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.JsonModels;
using NetCord.JsonModels;
using NetCord.Rest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DiscordConversationHistory.given;

public class a_discord_history : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);
    static readonly JsonSerializerOptions _json = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString, Converters = { new JsonStringEnumConverter() } };
    protected ConversationLocation location { get; } = new(1, 2, Snowflake(Now), Now);
    protected IList<string> requests { get; } = [];
    protected string messages { get; set; } = "[]";
    protected GatewayClient client { get; private set; } = null!;
    protected DiscordConversationHistory history { get; private set; } = null!;

    protected static ulong Snowflake(DateTimeOffset time) => (ulong)(time.ToUnixTimeMilliseconds() - 1420070400000) << 22;

    protected void Configure(ChannelType type = ChannelType.TextGuildChannel, Permissions permissions = Permissions.ViewChannel, string overwrites = "[]", ScopeOptions? scope = null)
    {
        scope ??= new();
        var token = Substitute.For<IEntityToken>();
        token.Id.Returns(42UL);
        var handler = Substitute.For<IRestRequestHandler>();
        handler.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var path = call.Arg<HttpRequestMessage>().RequestUri!.PathAndQuery;
            requests.Add(path);
            var body = path.Contains("/messages", StringComparison.Ordinal) ? messages :
                $$$"""{"id":"{{{scope.ChannelId}}}","guild_id":"{{{scope.GuildId}}}","type":{{{(int)type}}},"name":"scope","parent_id":"3","permission_overwrites":{{{overwrites}}},"thread_metadata":{"archived":false,"auto_archive_duration":60,"archive_timestamp":"2026-06-05T12:00:00Z","locked":false}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        });
        var parent = JsonSerializer.Deserialize<JsonChannel>(
            $$"""{"id":"3","guild_id":"1","type":0,"name":"parent","permission_overwrites":{{scope.ParentOverwrites}}}""",
            _json)!;
        var guild = new JsonGuild
        {
            Id = 1, Name = "guild", Roles = [new() { Id = 1, Name = "@everyone", Permissions = permissions }],
            Channels = [parent], ActiveThreads = [], Users = [], VoiceStates = [], Presences = [],
            Emojis = [], Features = [], StageInstances = [], Stickers = [], ScheduledEvents = []
        };
        client = new(token, new()
        {
            CacheProvider = ImmutableGatewayClientCacheProvider.FromJson(new JsonGatewayClientCache
            {
                User = new() { Id = 42, Username = "bot", IsBot = true }, Guilds = scope.IncludeGuild ? [guild] : []
            }),
            RestClientConfiguration = new() { RequestHandler = handler }
        });
        history = new(client);
    }

    protected sealed record ScopeOptions
    {
        public ulong GuildId { get; init; } = 1;
        public ulong ChannelId { get; init; } = 2;
        public bool IncludeGuild { get; init; } = true;
        public string ParentOverwrites { get; init; } = "[]";
    }
}
