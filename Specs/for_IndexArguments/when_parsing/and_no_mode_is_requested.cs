// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexArguments.when_parsing;

public class and_no_mode_is_requested : Specification
{
    IndexRequest _request = null!;
    void Because() => _request = IndexArguments.Parse(["index"]);

    [Fact] void should_remain_incremental() => _request.Full.ShouldBeFalse();
    [Fact] void should_not_consume_a_scheduled_slot() => _request.Scheduled.ShouldBeFalse();
}
