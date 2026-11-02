// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_prose_contains_mentions : Specification
{
    string _result = string.Empty;

    void Because() => _result = PublishedMarkdown.Neutralize("Ask @someone or @Cratis/team, not person@example.invalid.", new());

    [Fact] void should_make_user_and_team_mentions_inert() => _result.ShouldEqual("Ask `@someone` or `@Cratis/team`, not person@example.invalid.");
    [Fact] void should_neutralize_mentions_even_in_allowed_link_labels() => PublishedMarkdown.Neutralize("[@someone](https://cratis.io/arc/)", new()).ShouldEqual("`@someone` https://cratis.io/arc/");
    [Fact] void should_keep_transformed_allowed_labels_idempotent() => PublishedMarkdown.Neutralize("`@someone` https://cratis.io/arc/", new()).ShouldEqual("`@someone` https://cratis.io/arc/");
    [Fact] void should_be_idempotent() => PublishedMarkdown.Neutralize(_result, new()).ShouldEqual(_result);
}
