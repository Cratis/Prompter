// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DocsGapDigest.when_posting_once;

public class and_reading_fails : given.a_digest
{
    void Establish() => Interactions.Read(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<DocsGapRow>>(new IOException("unavailable")));
    async Task Because() => Posted = await Digest.PostOnce(Slot, CancellationToken.None);
    [Fact] void should_swallow_failure() => Posted.ShouldBeFalse();
    [Fact] void should_not_post() => Channel.DidNotReceiveWithAnyArgs().Post(default, default!, Arg.Any<CancellationToken>());
}
