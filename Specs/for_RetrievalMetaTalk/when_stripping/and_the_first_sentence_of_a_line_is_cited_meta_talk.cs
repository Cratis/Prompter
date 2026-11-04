// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_the_first_sentence_of_a_line_is_cited_meta_talk : Specification
{
    const string Text = "The excerpts don't cover tracing [3]. Fundamentals ships metrics [1].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_carry_the_citation_to_the_next_kept_sentence() => _result.ShouldEqual("Fundamentals ships metrics [1] [3].");
}
