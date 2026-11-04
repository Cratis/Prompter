// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_language_variants_crowd_the_default_page : given.a_corpus_with_client_language_variants
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(VariantQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_the_default_page_and_no_variant_page()
    {
        RequireDatabase();
        retrieved.Any(passage => passage.Page.Value == DefaultPage).ShouldBeTrue();
        retrieved.Any(passage => passage.Page.Value.StartsWith(VariantPagePrefix, StringComparison.Ordinal)).ShouldBeFalse();
    }
}
