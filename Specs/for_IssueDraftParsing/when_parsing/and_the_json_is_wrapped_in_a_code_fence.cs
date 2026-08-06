// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftParsing.when_parsing;

public class and_the_json_is_wrapped_in_a_code_fence : Specification
{
    IssueDraft _draft = null!;

    void Because() => _draft = IssueDraftParsing.Parse(
        """
        Here you go:

        ```json
        {"title":"A title","body":"A body","kind":"feature","product":"arc"}
        ```
        """)!;

    [Fact] void should_still_read_the_draft() => _draft.ShouldNotBeNull();
    [Fact] void should_read_the_title() => _draft.Title.ShouldEqual("A title");
    [Fact] void should_read_the_kind() => _draft.Kind.ShouldEqual(IssueKind.Feature);
}
