// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ClientLanguages.when_excluding_variants;

public class and_no_language_is_named : Specification
{
    const string Docs = "https://www.cratis.io";
    static readonly string[] _paths = ["chronicle/clients/elixir", "chronicle/clients/kotlin", "chronicle/clients/typescript", "chronicle/go", "arc/go", "arc/backend/kotlin", "arc/backend/elixir", "fundamentals/go", "eventmodelers-ai/kotlin", "eventmodelers-ai/java"];
    static readonly string[] _paths1 = ["https://www.cratis.io/chronicle/go.md", "https://cratis.io/chronicle/go.md", "https://cratis.io/arc/go.md"];
    IReadOnlyList<string> _excluded = [];

    static bool Has(IReadOnlyList<string> excluded, string path) => excluded.Contains($"{Docs}/{path}/") || excluded.Contains($"https://cratis.io/{path}/");

    void Because() => _excluded = ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question("how do I handle concurrency?")), Docs);

    [Fact] void should_exclude_every_secondary_variant() => _paths.All(path => Has(_excluded, path)).ShouldBeTrue();
    [Fact] void should_exclude_the_markdown_forms_on_both_hosts() => _paths1.All(_excluded.Contains).ShouldBeTrue();
    [Fact] void should_keep_the_typescript_arc_backend() => Has(_excluded, "arc/backend/typescript").ShouldBeFalse();
    [Fact] void should_keep_the_dotnet_pages() => _excluded.Any(prefix => prefix.Contains("csharp") || prefix.Contains("dotnet")).ShouldBeFalse();
}
