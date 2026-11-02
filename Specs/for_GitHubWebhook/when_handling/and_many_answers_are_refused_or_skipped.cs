// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_many_answers_are_refused_or_skipped : given.a_verified_delivery
{
    int _beforeRefill;

    void Establish()
    {
        Options.Protection.WebhookNotificationsPerMinute = 3;
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Answer.Refusal(0, []));
        Create();
    }

    async Task Because()
    {
        for (var number = 1; number <= 20; number++)
        {
            if (number == 3) Options.GitHub.AnsweringRepositories = [];
            await Webhook.Handle(Issue(number), default);
        }
        _beforeRefill = Channel.ReceivedCalls().Count();
        Clock.Advance(TimeSpan.FromMinutes(1));
        await Webhook.Handle(Issue(21), default);
    }

    [Fact] void should_bound_notifications_independently_of_paid_answers() => _beforeRefill.ShouldEqual(3);
    [Fact] void should_refill_notification_capacity() => Channel.ReceivedCalls().Count().ShouldEqual(4);
    [Fact] void should_never_comment_a_refusal() => Issues.ReceivedCalls().ShouldBeEmpty();
}
