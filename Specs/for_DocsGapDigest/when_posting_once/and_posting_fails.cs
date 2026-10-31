// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_DocsGapDigest.when_posting_once;

public class and_posting_fails : given.a_digest
{
    void Establish() => Channel.Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("unavailable")));
    async Task Because() => Posted = await Digest.PostOnce(Slot, CancellationToken.None);
    [Fact] void should_swallow_failure() => Posted.ShouldBeFalse();
}
