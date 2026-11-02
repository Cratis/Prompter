// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Digest;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Operations;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.given;

public class a_verified_delivery : Specification
{
    protected IAnswers Answers { get; } = Substitute.For<IAnswers>();
    protected IIssues Issues { get; } = Substitute.For<IIssues>();
    protected IMaintainerChannel Channel { get; } = Substitute.For<IMaintainerChannel>();
    protected IIssueDeliveries Deliveries { get; } = Substitute.For<IIssueDeliveries>();
    protected PrompterOptions Options { get; } = new();
    protected ManualClock Clock { get; } = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    protected GitHubWebhook Webhook { get; private set; } = null!;

    protected static OpenedIssue Issue(int number) => new("Cratis/Arc", number, "question", "details", $"https://github.com/Cratis/Arc/issues/{number}", false, []);

    protected void Create() => Webhook = new(Answers, Issues, Channel, Deliveries, Microsoft.Extensions.Options.Options.Create(Options), Clock, NullLogger<GitHubWebhook>.Instance);

    void Establish()
    {
        Options.GitHub.AnsweringRepositories = ["Cratis/Arc"];
        Options.GitHub.NotifyChannelId = 123;
        Deliveries.Claim(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new Answer("grounded", [], 1, false, []));
    }
}
