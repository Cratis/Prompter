// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalCommand.when_selecting_cases;

public class and_skip_is_given : Specification
{
    static readonly EvalSet _set = new() { Questions = [.. Enumerable.Range(1, 5).Select(number => new EvalCase { Id = $"arc-00{number}" })] };

    string[] _ids = [];

    void Because() => _ids = [.. EvalCommand.Select(_set, new EvalArguments(true, Skip: 2)).Select(item => item.Id)];

    [Fact] void should_skip_the_first_cases_in_file_order() => _ids.ShouldEqual(["arc-003", "arc-004", "arc-005"]);
}
