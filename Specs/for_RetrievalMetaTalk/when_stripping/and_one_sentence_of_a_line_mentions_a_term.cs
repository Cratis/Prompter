// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_one_sentence_of_a_line_mentions_a_term : Specification
{
    const string Text = "I couldn't find complete pricing info in the excerpts. Chronicle costs nothing to self-host [1].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_remove_only_the_offending_sentence() => _result.ShouldEqual("Chronicle costs nothing to self-host [1].");
}
