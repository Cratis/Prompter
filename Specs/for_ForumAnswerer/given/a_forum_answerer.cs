// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Routing;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord.Rest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.given;

public class a_forum_answerer : for_PaidWork.given.a_paid_work
{
    protected const ulong Forum = 7;
    protected const ulong ThreadId = 70;
    protected const ulong Owner = 3;
    protected static readonly ForumThreadRef Post = new(1, ThreadId, Forum, Owner);

    RestClient _client = null!;

    protected RecordingDiscordRest Rest { get; private set; } = null!;
    protected IAnswers Answers { get; private set; } = null!;
    protected ForumFollowUps FollowUps { get; private set; } = null!;
    protected ForumAnswerer Answerer { get; private set; } = null!;

    protected IEnumerable<string> Contents => Rest.Sent.Select(message => message.GetProperty("content").GetString()!);

    protected string Source() => (string)Answers.ReceivedCalls().Single().GetArguments()[1]!;

    void Establish()
    {
        Options.Protection.AllowedGuilds = [1];
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Rest = new();
        _client = Rest.Client();
        Answers = Substitute.For<IAnswers>();
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new Answer("Append it with the event log.", [], 0.9, false, []));
        FollowUps = new(config, Clock);
        var assistance = new ConversationAssistance(Substitute.For<IRequestClassifier>(), Answers, Substitute.For<IRelatedIssues>(), config);
        Answerer = new(
            _client,
            assistance,
            new(config, new RateLimiter(config), Clock),
            Work,
            new(Substitute.For<IConversationHistory>(), Clock),
            FollowUps,
            config,
            NullLogger<ForumAnswerer>.Instance);
    }

    void Destroy() => _client.Dispose();
}
