// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexBudget.when_preflighting;

public class and_inputs_require_retries : Specification
{
    IndexExposure _budget = null!;
    void Because()
    {
        var options = new PrompterOptions
        {
            Voyage = { MaxRetries = 0, IndexMaxRetries = 2 },
            Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 1, MaxDailyDollars = 1 }
        };
        _budget = IndexBudget.Preflight(["é"], options, new IndexRequest());
    }

    [Fact] void should_use_utf8_bytes_plus_framing_for_every_attempt() => _budget.Tokens.ShouldEqual(198);
    [Fact] void should_reserve_every_potential_attempt() => _budget.Dollars.ShouldEqual(0.000198m);
}
