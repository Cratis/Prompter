// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexBudget.when_preflighting;

public class and_the_full_run_ceiling_is_unset : Specification
{
    Exception? _error;
    void Because()
    {
        var options = new PrompterOptions { Ingestion = { PricePerMillionTokens = 0.06m, MaxRunDollars = 0.25m, MaxDailyDollars = 0.50m } };
        _error = Catch.Exception(() => IndexBudget.Preflight(["text"], options, new(true, "FULL REINDEX")));
    }

    [Fact] void should_disable_paid_full_runs() => _error.ShouldBeOfExactType<IndexBudgetExceeded>();
}
