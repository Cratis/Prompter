// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_similar_issues_exist : Specification
{
    string _advisory = null!;

    void Because() => _advisory = IssuePreview.Advisory([new(42, "A title", "https://github.com/Cratis/Chronicle/issues/42")]);

    [Fact] void should_keep_advice_separate_from_the_exact_payload() => _advisory.ShouldContain("possibly related");
    [Fact] void should_include_the_number() => _advisory.ShouldContain("#42");
    [Fact] void should_show_no_posting_buttons_itself() => _advisory.ShouldNotContain("File issue");
}
