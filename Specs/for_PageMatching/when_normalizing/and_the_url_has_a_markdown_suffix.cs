// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_PageMatching.when_normalizing;

public class and_the_url_has_a_markdown_suffix : Specification
{
    string _result = null!;

    void Because() => _result = PageMatching.Normalize("https://cratis.io/chronicle/events/appending.md");

    [Fact] void should_strip_the_md_suffix() => _result.ShouldEqual("https://cratis.io/chronicle/events/appending");
}
