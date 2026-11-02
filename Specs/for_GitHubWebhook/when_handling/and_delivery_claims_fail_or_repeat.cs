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

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_delivery_claims_fail_or_repeat : Specification
{
    readonly IAnswers _answers = Substitute.For<IAnswers>();
    readonly IIssues _issues = Substitute.For<IIssues>();
    readonly IMaintainerChannel _channel = Substitute.For<IMaintainerChannel>();
    readonly IIssueDeliveries _deliveries = Substitute.For<IIssueDeliveries>();

    async Task Because()
    {
        var options = new PrompterOptions();
        options.GitHub.AnsweringRepositories = ["Cratis/Arc"];
        options.GitHub.NotifyChannelId = 123;
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var webhook = new GitHubWebhook(_answers, _issues, _channel, _deliveries, Microsoft.Extensions.Options.Options.Create(options), clock, NullLogger<GitHubWebhook>.Instance);
        await webhook.Handle(new("Cratis/Arc", 1, "question", "details", "https://github.com/Cratis/Arc/issues/1", false, []), default);
        _deliveries.Claim(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new OperationCanceledException()));
        await webhook.Handle(new("Cratis/Arc", 2, "question", "details", "https://github.com/Cratis/Arc/issues/2", false, []), default);
    }

    [Fact] void should_not_answer() => _answers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_comment() => _issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify() => _channel.ReceivedCalls().ShouldBeEmpty();
}
