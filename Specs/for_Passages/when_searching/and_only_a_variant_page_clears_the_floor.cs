// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_only_a_variant_page_clears_the_floor : given.a_corpus_with_client_language_variants
{
    protected override float DefaultSimilarity => 0f;

    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(VariantQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_fall_back_to_the_variant_pages()
    {
        RequireDatabase();
        retrieved.Any(passage => passage.Page.Value.StartsWith(VariantPagePrefix, StringComparison.Ordinal)).ShouldBeTrue();
    }
}
