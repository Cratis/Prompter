// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.JsonModels;
using NetCord.Rest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.given;

public class an_admitted_message : for_PaidWork.given.a_paid_work
{
    GatewayClient _client = null!;

    protected IConversationHistory History { get; private set; } = null!;
    protected IAnswers Answers { get; private set; } = null!;
    protected IRequestClassifier Classifier { get; private set; } = null!;
    protected Admission Admission { get; private set; } = null!;
    protected Mentions Mentions { get; private set; } = null!;
    protected Message Message { get; set; } = null!;
    protected ForumFollowUps FollowUps { get; private set; } = null!;
    protected RestClient Rest => _client.Rest;

    protected static JsonMessage Json(ulong id, ulong channelId, ulong authorId, string content, bool authorIsBot = false) => new()
    {
        Id = id,
        GuildId = 1,
        ChannelId = channelId,
        Content = content,
        Author = new JsonUser { Id = authorId, Username = "spec-author", IsBot = authorIsBot },
        MentionedUsers = [],
        MentionedRoleIds = [],
        MentionedChannels = [],
        Attachments = [],
        Embeds = [],
        Components = [],
        Stickers = [],
        Reactions = [],
        MessageSnapshots = []
    };

    protected Message Build(
        ulong id,
        ulong channelId,
        ulong authorId,
        string content,
        bool authorIsBot = false,
        JsonMessage? referenced = null,
        MessageReferenceType referenceType = MessageReferenceType.Reply,
        TextChannel? channel = null,
        ulong? webhookId = null)
    {
        var json = Json(id, channelId, authorId, content, authorIsBot);
        json.WebhookId = webhookId;
        if (referenced is not null)
        {
            json.Type = MessageType.Reply;
            json.MessageReference = new JsonMessageReference { Type = referenceType, MessageId = referenced.Id, ChannelId = channelId };
            json.ReferencedMessage = referenced;
        }

        return new(json, null, channel, _client.Rest);
    }

    protected GuildThread Thread(ulong id = 70, ulong parent = 7, ulong owner = 3) =>
        GuildThread.CreateFromJson(new JsonChannel { Id = id, Type = ChannelType.PublicGuildThread, GuildId = 1, ParentId = parent, OwnerId = owner }, _client.Rest);

    protected string Source() => (string)Answers.ReceivedCalls().Single().GetArguments()[1]!;

    protected Question Asked() => (Question)Answers.ReceivedCalls().Single().GetArguments()[0]!;

    protected ConversationMessage[] Conversation() => ((Question)Answers.ReceivedCalls().Single().GetArguments()[0]!).Conversation.Messages.ToArray();

    void Establish()
    {
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.AskChannelId = 2;
        Options.Discord.HelpForumChannelId = 7;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Admission = new(config, new RateLimiter(config), Clock);

        // Suppress refusal replies: these specs must never make a Discord request.
        Admission.MayReply(new(1, 2, 3));
        Admission.MayReply(new(1, 5, 3));
        Admission.MayReply(new(1, 70, 3));
        var token = Substitute.For<IEntityToken>();
        token.Id.Returns(42UL);
        _client = new(token, new() { RestClientConfiguration = new() { RequestHandler = Substitute.For<IRestRequestHandler>() } });
        History = Substitute.For<IConversationHistory>();
        Answers = Substitute.For<IAnswers>();
        Classifier = Substitute.For<IRequestClassifier>();
        var assistance = new ConversationAssistance(Classifier, Answers, Substitute.For<IRelatedIssues>(), config);
        FollowUps = new(config, Clock);
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromCanceled<Answer>(new(true)));
        Mentions = new(_client, new(_client.Rest, config, NullLogger<AnswerDelivery>.Instance), assistance, Admission, Work, new(History, Clock), FollowUps, config, NullLogger<Mentions>.Instance);
        Message = Build(100, 2, 3, "current question");
    }
}
