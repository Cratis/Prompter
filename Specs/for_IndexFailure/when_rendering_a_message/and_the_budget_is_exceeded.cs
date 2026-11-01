// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexFailure.when_rendering_a_message;

public class and_the_budget_is_exceeded : Specification
{
    string _message = null!;

    void Because() => _message = IndexFailure.Message(new IndexBudgetExceeded());

    [Fact] void should_name_the_failure_kind() => _message.ShouldContain("(IndexBudgetExceeded)");
    [Fact] void should_carry_the_cause() => _message.ShouldContain("Index budget unavailable or exceeded");
    [Fact] void should_exit_with_an_ordinary_failure_code() => IndexFailure.ExitCode.ShouldEqual(1);
}
