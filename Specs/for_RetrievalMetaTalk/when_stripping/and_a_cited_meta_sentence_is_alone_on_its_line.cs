// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_a_cited_meta_sentence_is_alone_on_its_line : Specification
{
    const string Text = "Chronicle stores events [1].\nThe passage shows it [2].\nArc builds on it [3].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_remove_that_line_and_keep_the_others() => _result.ShouldEqual("Chronicle stores events [1].\nArc builds on it [3].");
}
