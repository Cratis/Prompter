// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexBudget.when_preflighting;

public class and_a_full_corpus_is_confirmed : Specification
{
    readonly string[] _inputs = Enumerable.Repeat(new string('x', 4_000), 5_575).ToArray();
    readonly PrompterOptions _options = new()
    {
        Ingestion = { PricePerMillionTokens = 0.06m, MaxFullRunDollars = 1, MaxFullTokens = 16_000_000, MaxRunDollars = 0.25m, MaxDailyDollars = 0.50m }
    };
    IndexExposure _admission = null!;
    Exception? _incremental;

    void Because()
    {
        _admission = IndexBudget.Preflight(_inputs, _options, new(true, "FULL REINDEX"));
        _incremental = Catch.Exception(() => IndexBudget.Preflight(_inputs, _options, new()));
    }

    [Fact] void should_admit_the_full_corpus() => (_admission.Dollars < 0.60m).ShouldBeTrue();
    [Fact] void should_refuse_the_same_incremental_corpus() => _incremental.ShouldBeOfExactType<IndexBudgetExceeded>();
}
