// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_a_claimed_attempt_fails : given.a_verified_delivery
{
    void Establish()
    {
        Deliveries.Claim(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true, false);
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Answer>(new OperationCanceledException()));
        Create();
    }

    async Task Because()
    {
        await Webhook.Handle(Issue(1), default);
        Create(); // A fresh process coordinator still consults the same durable claim store.
        await Webhook.Handle(Issue(1), default);
    }

    [Fact] void should_not_retry_the_claimed_answer_on_redelivery() => Answers.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_not_comment_after_failure() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_repeat_the_first_attempt_notification() => Channel.ReceivedCalls().Count().ShouldEqual(1);
}
