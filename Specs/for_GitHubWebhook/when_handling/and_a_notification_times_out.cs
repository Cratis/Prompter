// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubWebhook.when_handling;

public class and_a_notification_times_out : given.a_verified_delivery
{
    CancellationToken _received;
    int _posts;

    void Establish()
    {
        Channel.Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _posts++;
            if (_posts == 1)
            {
                _received = call.Arg<CancellationToken>();
                await new TaskCompletionSource().Task.WaitAsync(_received);
            }
        });
        Create();
    }

    async Task Because()
    {
        var pending = Webhook.Handle(Issue(1), default);
        Clock.Advance(TimeSpan.FromSeconds(2));
        await pending;
        await Webhook.Handle(Issue(2), default);
    }

    [Fact] void should_cancel_the_notification_token() => _received.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_release_the_notification_permit() => _posts.ShouldEqual(2);
}
