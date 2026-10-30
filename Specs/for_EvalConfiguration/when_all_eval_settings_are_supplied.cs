// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval;

namespace Cratis.Prompter.Specs.for_EvalConfiguration;

public class when_all_eval_settings_are_supplied : Specification
{
    EvalConfiguration _configuration = null!;

    void Establish() => _configuration = new()
    {
        ConnectionString = "synthetic", AnthropicApiKey = "synthetic", VoyageApiKey = "synthetic",
        AnswerModel = "fixture", JudgeModel = "fixture", EmbeddingModel = "fixture", CorpusManifest = "fixture"
    };

    [Fact] void should_require_explicit_approval() => _configuration.PaidBlockReason.ShouldEqual("Explicit paid approval is required.");
    [Fact] void should_block_even_approved_execution_until_accounting_is_integrated() =>
        (_configuration with { PaidApproved = true }).PaidBlockReason.ShouldEqual("Paid evaluation is disabled: aggregate budget/admission integration is pending.");
}
