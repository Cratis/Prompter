// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_the_text_has_no_meta_talk : Specification
{
    const string Text = "Chronicle is the event store [1]. Arc is the CQRS framework [2].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_leave_the_text_unchanged() => _result.ShouldEqual(Text);
}
