// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

/// <summary>Preserves lease cancellation forwarding, settlement, and the apology path.</summary>
public class and_answering_is_canceled : given.a_forum_answerer
{
    bool _usesLeaseToken;

    void Establish() => Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
    {
        var token = call.Arg<CancellationToken>();
        _usesLeaseToken = token.CanBeCanceled && token == WorkLease.Current!.Token;
        return Task.FromCanceled<Answer>(new CancellationToken(true));
    });

    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: true);

    [Fact] void should_forward_the_active_lease_token() => _usesLeaseToken.ShouldBeTrue();
    [Fact] void should_send_only_the_error_reply() => Contents.ShouldEqual([Options.Discord.ErrorReply]);
    [Fact] void should_not_track_follow_ups() => FollowUps.Accepts(ThreadId).ShouldBeFalse();
    [Fact] void should_settle_the_lease() => Ledger.Received(1).Reconcile(Arg.Any<Guid>(), Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>());
}
