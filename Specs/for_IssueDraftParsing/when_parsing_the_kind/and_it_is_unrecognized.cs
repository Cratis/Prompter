// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftParsing.when_parsing_the_kind;

public class and_it_is_unrecognized : Specification
{
    [Fact] void should_claim_the_least_by_calling_it_an_idea() => IssueDraftParsing.ParseKind("wishlist").ShouldEqual(IssueKind.Idea);
    [Fact] void should_treat_missing_the_same_way() => IssueDraftParsing.ParseKind(null).ShouldEqual(IssueKind.Idea);
}
