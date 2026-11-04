// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ClientLanguages.when_excluding_variants;

public class and_a_user_message_in_the_conversation_names_a_language : Specification
{
    const string Docs = "https://www.cratis.io";
    IReadOnlyList<string> _excluded = [];

    static bool Has(IReadOnlyList<string> excluded, string path) => excluded.Contains($"{Docs}/{path}/") || excluded.Contains($"https://cratis.io/{path}/");

    void Because() => _excluded = ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question("and how about concurrency?") { Conversation = new([new("user", "I am building with Elixir"), new("assistant", "Great. Do you use Kotlin?")]) }), Docs);

    [Fact] void should_keep_the_pages_of_the_language_the_user_named() => Has(_excluded, "chronicle/clients/elixir").ShouldBeFalse();
    [Fact] void should_not_count_a_language_only_an_assistant_message_mentions() => Has(_excluded, "chronicle/clients/kotlin").ShouldBeTrue();
}
