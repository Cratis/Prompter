// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval;

namespace Cratis.Prompter.Specs.for_OfflineEvaluation;

public class when_scoring_fixtures : Specification
{
    EvalReport _report = null!;

    void Because() => _report = OfflineEvaluation.Run("revision-fixture");

    [Fact] void should_be_complete() => _report.Complete.ShouldBeTrue();
    [Fact] void should_be_reproducible() => _report.ToJson().ShouldEqual(OfflineEvaluation.Run("revision-fixture").ToJson());
    [Fact] void should_mark_synthetic_evidence() => _report.ToJson().Contains("synthetic", StringComparison.Ordinal).ShouldBeTrue();
}
