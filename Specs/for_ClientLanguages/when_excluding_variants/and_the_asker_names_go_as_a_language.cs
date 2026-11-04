// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ClientLanguages.when_excluding_variants;

public class and_the_asker_names_go_as_a_language : Specification
{
    const string Docs = "https://www.cratis.io";
    static readonly string[] _paths = ["chronicle/go", "arc/go", "fundamentals/go"];
    IReadOnlyList<string> _excluded = [];

    static bool Has(IReadOnlyList<string> excluded, string path) => excluded.Contains($"{Docs}/{path}/") || excluded.Contains($"https://cratis.io/{path}/");

    void Because() => _excluded = ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question("how do I connect from Go? and with the Go client?")), Docs);

    [Fact] void should_keep_the_go_pages() => _paths.Any(path => Has(_excluded, path)).ShouldBeFalse();
    [Fact] void should_exclude_the_other_variants() => Has(_excluded, "chronicle/clients/elixir").ShouldBeTrue();
}
