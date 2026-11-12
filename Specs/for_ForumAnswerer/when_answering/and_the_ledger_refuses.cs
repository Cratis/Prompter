// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ForumAnswerer.when_answering;

public class and_the_ledger_refuses : given.a_forum_answerer
{
    void Establish() => Ledger.Reserve(Arg.Any<Protection.SpendExposure>(), Arg.Any<CancellationToken>()).Returns(_ => (Guid?)null);

    async Task Because() => await Answerer.Answer(Post, Forum, null, delayed: false);

    [Fact] void should_send_only_the_rate_limited_reply() => Contents.SequenceEqual([Options.Discord.RateLimitedReply]).ShouldBeTrue();
    [Fact] void should_mention_nobody() => Rest.MentionedNobody.ShouldBeTrue();
}
