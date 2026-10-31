// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalSet.when_loading;

public class and_the_golden_set_is_read : Specification
{
    EvalSet _set = null!;

    void Because() => _set = EvalSet.Load(EvalSet.DefaultPath(AppContext.BaseDirectory)!);

    [Fact] void should_read_all_cases() => _set.Questions.Count.ShouldEqual(69);
    [Fact] void should_keep_ids_unique() => _set.Questions.Select(item => item.Id).Distinct().Count().ShouldEqual(69);
    [Fact] void should_require_facts_in_at_least_fifteen_cases() => (_set.Questions.Count(item => item.MustMention.Count > 0) >= 15).ShouldBeTrue();
}
