// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_chunking_profile_is_checked : given.an_indexer
{
    async Task Because() => await Indexer.Run();

    [Fact] async Task should_check_the_markdown_v2_profile() => await Lease.Received().CheckProfile(Arg.Is<string>(profile => profile.EndsWith("/markdown-v2", StringComparison.Ordinal)), false, Arg.Any<CancellationToken>());
}
