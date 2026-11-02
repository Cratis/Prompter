// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_prose_contains_mentions : Specification
{
    string _result = string.Empty;

    void Because() => _result = PublishedMarkdown.Neutralize("Ask @someone or @Cratis/team, @everyone, @here, <@1> <@!2> <@&3> <#4> </cmd:5>.", new());

    [Fact] void should_break_every_mention() => _result.ShouldEqual("Ask \uFF20someone or \uFF20Cratis/team, \uFF20everyone, \uFF20here, \uFF1C\uFF201> \uFF1C\uFF20!2> \uFF1C\uFF20&3> \uFF1C#4> \uFF1C/cmd[:]5>.");
    [Fact] void should_break_mentions_in_allowed_link_labels() => PublishedMarkdown.Neutralize("[@someone](https://cratis.io/arc/)", new()).ShouldEqual("[\uFF20someone](https://cratis.io/arc/)");
    [Fact] void should_be_idempotent() => PublishedMarkdown.Neutralize(_result, new()).ShouldEqual(_result);
}
