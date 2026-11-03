// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_checking_mentions;

public class and_the_term_is_hyphenated_or_differently_cased : Specification
{
    [Fact] void should_match_regardless_of_case() => RetrievalMetaTalk.Mentions("The EXCERPT says Chronicle is the event store.", "excerpt").ShouldBeTrue();
    [Fact] void should_match_a_hyphenated_occurrence() => RetrievalMetaTalk.Mentions("The provided-documentation lists every product.", "the provided documentation").ShouldBeTrue();
    [Fact] void should_not_match_an_unrelated_term() => RetrievalMetaTalk.Mentions("Chronicle is the event store [1].", "excerpt").ShouldBeFalse();
    [Fact] void should_detect_any_configured_term() => RetrievalMetaTalk.MentionsAny("This passage covers Arc.").ShouldBeTrue();
    [Fact] void should_report_no_term_when_none_mentioned() => RetrievalMetaTalk.MentionsAny("Chronicle is the event store [1].").ShouldBeFalse();
}
