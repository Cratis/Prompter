// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalCommand.when_selecting_cases;

public class and_nothing_is_given : Specification
{
    static readonly EvalSet _set = new() { Questions = [.. Enumerable.Range(1, 5).Select(number => new EvalCase { Id = $"arc-00{number}" })] };

    int _count;

    void Because() => _count = EvalCommand.Select(_set, new EvalArguments(true)).Count();

    [Fact] void should_select_every_case() => _count.ShouldEqual(5);
}
