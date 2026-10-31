// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_DocsGapDigest.when_posting_once;

public class and_no_channel_is_configured : given.a_digest
{
    void Establish() => Options.Discord.MaintainerChannelId = null;
    async Task Because() => Posted = await Digest.PostOnce(Slot, CancellationToken.None);
    [Fact] void should_not_post() => Posted.ShouldBeFalse();
    [Fact] void should_not_query() => Interactions.DidNotReceiveWithAnyArgs().Read(default, default, Arg.Any<CancellationToken>());
    [Fact] void should_not_contact_discord() => Channel.DidNotReceiveWithAnyArgs().Post(default, default!, Arg.Any<CancellationToken>());
}
