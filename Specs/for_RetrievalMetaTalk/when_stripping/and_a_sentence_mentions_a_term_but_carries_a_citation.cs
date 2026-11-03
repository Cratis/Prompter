// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_a_sentence_mentions_a_term_but_carries_a_citation : Specification
{
    const string Text = "The excerpts describe Chronicle as the event store [1].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_keep_the_sentence_so_the_citation_is_not_lost() => _result.ShouldEqual(Text);
}
