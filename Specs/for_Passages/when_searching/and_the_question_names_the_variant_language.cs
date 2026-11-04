// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_the_question_names_the_variant_language : given.a_corpus_with_client_language_variants
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(ElixirQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_the_variant_pages()
    {
        RequireDatabase();
        retrieved.Any(passage => passage.Page.Value.StartsWith(VariantPagePrefix, StringComparison.Ordinal)).ShouldBeTrue();
    }
}
