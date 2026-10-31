// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalSummary.when_writing_a_report_line;

public class and_results_exist : Specification
{
    const string Question = "How do I secretly configure the widget?";

    EvalSummary _summary = null!;
    string _line = string.Empty;
    JsonElement _report;

    void Establish()
    {
        var question = new EvalCase { Id = "safe-001", Question = Question, Expected = "answer" };
        var passed = new EvalResult(question.Id, true, true, true, true, true, true, 0.1m, ["https://cratis.io/arc/events.md"]);
        _summary = EvalSummary.From([passed, passed with { Id = "safe-002", Cost = 0.2m }], new EvalArguments(true));
    }

    void Because()
    {
        _line = _summary.ReportLine();
        _report = JsonDocument.Parse(_line[EvalSummary.ReportMarker.Length..]).RootElement;
    }

    [Fact] void should_start_with_the_marker() => _line.StartsWith(EvalSummary.ReportMarker, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_fit_on_one_line() => _line.Contains('\n').ShouldBeFalse();
    [Fact] void should_carry_the_pass_rate() => _report.GetProperty("PassRate").GetDouble().ShouldEqual(1d);
    [Fact] void should_carry_the_total_cost() => _report.GetProperty("TotalCost").GetDecimal().ShouldEqual(0.3m);
    [Fact] void should_carry_the_exit_code() => _report.GetProperty("ExitCode").GetInt32().ShouldEqual(0);
    [Fact] void should_carry_the_case_ids() => _report.GetProperty("Results")[0].GetProperty("Id").GetString().ShouldEqual("safe-001");
    [Fact] void should_not_carry_question_text() => _line.Contains(Question, StringComparison.Ordinal).ShouldBeFalse();
}
