// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_shutdown_occurs_during_claim_or_answer : given.a_verified_delivery
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_start_later_side_effects(bool duringAnswer)
    {
        using var shutdown = new CancellationTokenSource();
        if (duringAnswer)
        {
            Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                await shutdown.CancelAsync();
                return new Answer("grounded", [], 1, false, []);
            });
        }
        else
        {
            Deliveries.Claim(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                await shutdown.CancelAsync();
                return true;
            });
        }
        Create();
        var error = await Catch.Exception(() => Webhook.Handle(Issue(1), shutdown.Token));
        (error is OperationCanceledException).ShouldBeTrue();
        Answers.ReceivedCalls().Count().ShouldEqual(duringAnswer ? 1 : 0);
        Issues.ReceivedCalls().ShouldBeEmpty();
        Channel.ReceivedCalls().ShouldBeEmpty();
    }
}
