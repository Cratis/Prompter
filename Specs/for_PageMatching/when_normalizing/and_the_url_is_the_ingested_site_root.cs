// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Specs.for_PageMatching.when_normalizing;

public class and_the_url_is_the_ingested_site_root : Specification
{
    string _result = null!;

    void Because() => _result = PageMatching.Normalize("https://cratis.io/index.md");

    [Fact] void should_collapse_to_the_bare_root() => _result.ShouldEqual("https://cratis.io");
}
