// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_the_claim_deadline_expires : given.a_verified_delivery
{
    CancellationToken _received;

    void Establish()
    {
        Deliveries.Claim(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _received = call.Arg<CancellationToken>();
            await new TaskCompletionSource().Task.WaitAsync(_received);
            return true;
        });
        Create();
    }

    async Task Because()
    {
        var pending = Webhook.Handle(Issue(1), default);
        Clock.Advance(TimeSpan.FromSeconds(2));
        await pending;
    }

    [Fact] void should_cancel_the_received_claim_token() => _received.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_comment() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify() => Channel.ReceivedCalls().ShouldBeEmpty();
}
