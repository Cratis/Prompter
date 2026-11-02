// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_the_first_claim_succeeds : given.a_verified_delivery
{
    void Establish() => Create();
    Task Because() => Webhook.Handle(Issue(1), default);

    [Fact] async Task should_answer_once() => await Answers.Received(1).For(Arg.Any<Question>(), "github-issue", Arg.Any<CancellationToken>());
    [Fact] async Task should_comment_once() => await Issues.Received(1).Comment("Arc", 1, Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_notify_once() => await Channel.Received(1).Post(123, Arg.Any<string>(), Arg.Any<CancellationToken>());
}
