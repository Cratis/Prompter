// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_expansion_is_already_retrieved : given.a_grounded_answerer
{
    void Establish()
    {
        var detail = Passage(0.8) with { Content = "Capability map", HeadingPath = "Details" };
        Found(Passage(0.9) with { IsSummary = true, Content = "Summary" }, detail, detail with { IsPageExpansion = true });
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_not_duplicate_the_detail() => result.Passages.Count.ShouldEqual(2);
    [Fact] void should_keep_original_retrieval_order() => result.Passages.Select(passage => passage.Content).ShouldEqual(["Summary", "Capability map"]);
    [Fact] void should_keep_one_page_identity() => result.Citations.Count().ShouldEqual(1);
}
