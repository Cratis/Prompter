// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueAnswerComment.when_composing;

public class and_the_answer_has_no_sources : Specification
{
    string _comment = null!;

    void Because() => _comment = IssueAnswerComment.For(new("An answer.", [], 0.9, false, []), "no-prompter");

    [Fact] void should_not_show_an_empty_sources_heading() => _comment.Contains("**Sources**", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_still_attribute_itself() => _comment.ShouldContain("Prompter");
}
