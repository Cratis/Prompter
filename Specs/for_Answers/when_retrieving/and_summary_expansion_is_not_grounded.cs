// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_summary_expansion_is_not_grounded : given.a_grounded_answerer
{
    void Establish()
    {
        options.Answering.MinSemanticSimilarity = 0.5;
        Found(
            Passage(0.9) with { IsSummary = true, Content = "Admitted summary" },
            Passage(0.1) with { IsPageExpansion = true, Content = "Below floor" },
            Passage(double.NaN) with { IsPageExpansion = true, Content = "Nonfinite score" },
            Passage(1.1) with { IsPageExpansion = true, Content = "Invalid score" },
            Passage(0.8) with { Page = new("https://cratis.io/unadmitted/"), IsPageExpansion = true, Content = "Unadmitted page" },
            Passage(0.8) with { Page = new("https://untrusted.example/page"), IsPageExpansion = true, Content = "Untrusted page" });
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_keep_only_the_grounded_summary() => result.Passages.Select(passage => passage.Content).ShouldEqual(["Admitted summary"]);
    [Fact] void should_not_invent_missing_provenance() => result.CitationProvenance[0].Note.ShouldEqual("index date unknown");
}
