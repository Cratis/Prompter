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
    protected Message Message { get; private set; } = null!;

    void Establish()
    {
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.AskChannelId = 2;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Admission = new(config, new RateLimiter(config), Clock);

        // Suppress refusal replies: these specs must never make a Discord request.
        Admission.MayReply(new(1, 2, 3));
        var token = Substitute.For<IEntityToken>();
        token.Id.Returns(42UL);
        _client = new(token, new() { RestClientConfiguration = new() { RequestHandler = Substitute.For<IRestRequestHandler>() } });
        History = Substitute.For<IConversationHistory>();
        Answers = Substitute.For<IAnswers>();
        Classifier = Substitute.For<IRequestClassifier>();
        var assistance = new ConversationAssistance(Classifier, Answers, Substitute.For<IRelatedIssues>(), config);
        Mentions = new(_client, _client.Rest, assistance, Admission, Work, new(History, Clock), config, NullLogger<Mentions>.Instance);
        var json = new JsonMessage
        {
            Id = 100,
            GuildId = 1,
            ChannelId = 2,
            Content = "current question",
            Author = new JsonUser { Id = 3, Username = "spec-author" },
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
        Message = new(json, null, null, _client.Rest);
    }
}
