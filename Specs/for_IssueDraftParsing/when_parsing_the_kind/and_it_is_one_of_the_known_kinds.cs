// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftParsing.when_parsing_the_kind;

public class and_it_is_one_of_the_known_kinds : Specification
{
    [Fact] void should_read_a_bug() => IssueDraftParsing.ParseKind("bug").ShouldEqual(IssueKind.Bug);
    [Fact] void should_read_a_feature() => IssueDraftParsing.ParseKind("FEATURE").ShouldEqual(IssueKind.Feature);
    [Fact] void should_read_documentation() => IssueDraftParsing.ParseKind(" documentation ").ShouldEqual(IssueKind.Documentation);
    [Fact] void should_read_the_docs_shorthand() => IssueDraftParsing.ParseKind("docs").ShouldEqual(IssueKind.Documentation);
}
