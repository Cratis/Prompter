// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Eval;

namespace Cratis.Prompter.Specs.for_QuestionResult;

public class when_formatting_a_console_line : Specification
{
    readonly QuestionResult _result = new("case", "Arc", "question", false, false, true, true, 4.5, 0.8, [], null);
    string[] _lines = null!;

    void Because()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            _lines = [
                _result.ConsoleLine(),
                (_result with { Error = "failed" }).ConsoleLine(),
                (_result with { CitationHit = false, Groundedness = null }).ConsoleLine(),
                (_result with { ExpectedRefusal = true, ActualRefusal = true, CitationHit = null, Groundedness = null }).ConsoleLine(),
                (_result with { ActualRefusal = true, RefusalCorrect = false }).ConsoleLine(),
                (_result with { ExpectedRefusal = true, RefusalCorrect = false }).ConsoleLine()
            ];
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact] void should_format_a_pass_invariantly() => _lines[0].ShouldEqual("  [case] PASS  cite=hit refusal=answered grounded=4.5");
    [Fact] void should_prioritize_an_error() => _lines[1].ShouldEqual("  [case] FAIL  error: failed");
    [Fact] void should_format_a_missed_citation_without_a_judge() => _lines[2].ShouldEqual("  [case] FAIL  cite=miss refusal=answered grounded=n/a");
    [Fact] void should_format_an_expected_refusal() => _lines[3].ShouldEqual("  [case] PASS  cite=n/a refusal=ok grounded=n/a");
    [Fact] void should_format_an_unexpected_refusal() => _lines[4].ShouldEqual("  [case] FAIL  cite=hit refusal=refused grounded=4.5");
    [Fact] void should_format_an_unexpected_answer() => _lines[5].ShouldEqual("  [case] FAIL  cite=hit refusal=answered grounded=4.5");
}
