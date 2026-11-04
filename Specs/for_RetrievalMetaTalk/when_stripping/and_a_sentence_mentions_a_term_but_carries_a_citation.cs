// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_a_sentence_mentions_a_term_but_carries_a_citation : Specification
{
    const string Text = "Chronicle records metrics [1]. The excerpts below assume the illustrated service types [2].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_drop_the_meta_sentence_and_keep_its_citation() => _result.ShouldEqual("Chronicle records metrics [1] [2].");
}
