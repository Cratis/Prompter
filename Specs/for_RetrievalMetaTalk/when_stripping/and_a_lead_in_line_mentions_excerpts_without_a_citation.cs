// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_a_lead_in_line_mentions_excerpts_without_a_citation : Specification
{
    const string Text = "Based on the excerpts, here are the Cratis products:\n- Chronicle: event store [1]\n- Arc: CQRS framework [2]\n- Components: React UI [3]";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_remove_the_lead_in_line() => _result.ShouldEqual("- Chronicle: event store [1]\n- Arc: CQRS framework [2]\n- Components: React UI [3]");
}
