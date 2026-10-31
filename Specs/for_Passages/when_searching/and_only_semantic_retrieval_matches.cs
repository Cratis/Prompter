// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_only_semantic_retrieval_matches : given.a_disposable_documentation_corpus
{
    Answer _result = null!;

    async Task Because()
    {
        if (HasDatabase)
        {
            _result = await answers.For(new(PositiveQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_ground_the_answer_with_independent_real_sql_similarity()
    {
        RequireDatabase();
        var passage = retrieved[0];
        (Math.Abs(passage.Score - (1.0 / 61)) < 1e-9).ShouldBeTrue();
        (Math.Abs(passage.SemanticSimilarity - 0.8) < 1e-6).ShouldBeTrue();
        passage.Page.ShouldEqual(new PageUrl("https://cratis.io/chronicle/"));
        passage.Title.ShouldEqual("Chronicle");
        passage.HeadingPath.ShouldEqual("Overview");
        passage.Content.ShouldEqual(ChronicleContent);
        retrieved.Skip(1).All(candidate => Math.Abs(candidate.SemanticSimilarity) < 1e-6).ShouldBeTrue();
        _result.IsRefusal.ShouldBeFalse();
        _result.Passages.Single().ShouldEqual(passage);
        _result.Citations.Single().ShouldEqual(passage.Page);
        chat.ReceivedCalls().Count().ShouldEqual(1);
        sent[1].Text.ShouldContain(ChronicleContent);
        sent[1].Text.ShouldNotContain("Unrelated context.");
        embeddings.ReceivedCalls().Count().ShouldEqual(1);
        ((IEnumerable<string>)embeddings.ReceivedCalls().Single().GetArguments()[0]!).Single().ShouldEqual(PositiveQuestion);
    }
}
