// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ClientLanguages.when_excluding_variants;

public class and_typescript_is_named : Specification
{
    const string Docs = "https://www.cratis.io";
    IReadOnlyList<string> _excluded = [];

    static bool Has(IReadOnlyList<string> excluded, string path) => excluded.Contains($"{Docs}/{path}/") || excluded.Contains($"https://cratis.io/{path}/");

    void Because() => _excluded = ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question("how do I append events from a TypeScript app?")), Docs);

    [Fact] void should_keep_every_typescript_page() => (Has(_excluded, "chronicle/clients/typescript") || Has(_excluded, "arc/backend/typescript")).ShouldBeFalse();
    [Fact] void should_exclude_the_other_variants() => Has(_excluded, "chronicle/clients/elixir").ShouldBeTrue();
}
