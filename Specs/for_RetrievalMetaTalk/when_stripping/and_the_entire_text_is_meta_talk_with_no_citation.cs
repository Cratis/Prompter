// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_the_entire_text_is_meta_talk_with_no_citation : Specification
{
    const string Text = "I'm not sure the excerpts are relevant here.";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_leave_the_text_unchanged_rather_than_answer_with_nothing() => _result.ShouldEqual(Text);
}
