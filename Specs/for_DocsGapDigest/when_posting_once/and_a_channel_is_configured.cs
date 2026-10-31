// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_DocsGapDigest.when_posting_once;

public class and_a_channel_is_configured : given.a_digest
{
    async Task Because() => Posted = await Digest.PostOnce(Slot, CancellationToken.None);
    [Fact] void should_read_the_last_seven_days() => Interactions.Received(1).Read(Slot.AddDays(-7), Slot, CancellationToken.None);
    [Fact] void should_post_once_to_the_configured_channel() => Channel.Received(1).Post(123, Arg.Is<string>(text => text.StartsWith("**Prompter docs-gap digest**", StringComparison.Ordinal)), CancellationToken.None);
    [Fact] void should_report_success() => Posted.ShouldBeTrue();
}
