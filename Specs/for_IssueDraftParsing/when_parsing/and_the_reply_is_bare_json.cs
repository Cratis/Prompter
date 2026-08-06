// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftParsing.when_parsing;

public class and_the_reply_is_bare_json : Specification
{
    IssueDraft _draft = null!;

    void Because() => _draft = IssueDraftParsing.Parse(
        """{"title":"Projection stops after a rename","body":"Steps.","kind":"bug","product":"chronicle"}""")!;

    [Fact] void should_read_the_title() => _draft.Title.ShouldEqual("Projection stops after a rename");
    [Fact] void should_read_the_body() => _draft.Body.ShouldEqual("Steps.");
    [Fact] void should_read_the_kind() => _draft.Kind.ShouldEqual(IssueKind.Bug);
    [Fact] void should_read_the_product() => _draft.Product.ShouldEqual("chronicle");
}
