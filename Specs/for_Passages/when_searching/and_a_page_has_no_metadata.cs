// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_a_page_has_no_metadata : given.a_disposable_documentation_corpus
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(PositiveQuestion), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_the_passage_with_unknown_metadata()
    {
        RequireDatabase();
        var passage = retrieved[0];
        passage.IndexedAt.ShouldBeNull();
        passage.PageLastModifiedAt.ShouldBeNull();
        passage.ProductVersion.ShouldBeNull();
    }
}
