// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_a_citable_link_is_in_code_and_prose : Specification
{
    const string Markdown = "Read https://cratis.io/arc/ now.\n```\nvar url = https://cratis.io/arc/;\n```\n";

    [Fact] void should_keep_it_exactly() => PublishedMarkdown.Neutralize(Markdown, new()).ShouldEqual(Markdown);
}
