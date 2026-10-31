// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_lexical_overlap_is_not_grounding : given.a_disposable_documentation_corpus
{
    Answer _orthogonal = null!;
    Answer _lexicalOnly = null!;
    Answer _weak = null!;
    Passage _weakCandidate = null!;
    Passage _lexicalCandidate = null!;
    IReadOnlyList<Passage> _orthogonalCandidates = [];

    async Task Because()
    {
        if (HasDatabase)
        {
            _orthogonal = await answers.For(new(NegativeQuestion), "cli");
            _orthogonalCandidates = retrieved;
            _weak = await answers.For(new(WeakQuestion), "cli");
            _weakCandidate = retrieved[0];
            _lexicalOnly = await answers.For(new(LexicalOnlyQuestion), "cli");
            _lexicalCandidate = retrieved.Single(passage => passage.Page.Value == "https://cratis.io/glossary/");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_refuse_orthogonal_neighbors_and_lexical_only_matches_without_chat()
    {
        RequireDatabase();
        _orthogonal.IsRefusal.ShouldBeTrue();
        _orthogonal.Citations.Any().ShouldBeFalse();
        _orthogonalCandidates.All(passage => Math.Abs(passage.SemanticSimilarity) < 1e-6).ShouldBeTrue();
        (_orthogonalCandidates.Count > 0).ShouldBeTrue();
        _weak.IsRefusal.ShouldBeTrue();
        _weak.Citations.Any().ShouldBeFalse();
        (Math.Abs(_weakCandidate.Score - (2.0 / 61)) < 1e-9).ShouldBeTrue();
        (Math.Abs(_weakCandidate.SemanticSimilarity - 0.2) < 1e-6).ShouldBeTrue();
        _lexicalOnly.IsRefusal.ShouldBeTrue();
        _lexicalOnly.Citations.Any().ShouldBeFalse();
        (Math.Abs(_lexicalCandidate.Score - (1.0 / 61)) < 1e-9).ShouldBeTrue();
        (Math.Abs(_lexicalCandidate.SemanticSimilarity + 1) < 1e-6).ShouldBeTrue();
        chat.ReceivedCalls().Any().ShouldBeFalse();
        embeddings.ReceivedCalls().Count().ShouldEqual(3);
        var questions = embeddings.ReceivedCalls().Select(call => ((IEnumerable<string>)call.GetArguments()[0]!).Single()).ToArray();
        questions.SequenceEqual([NegativeQuestion, WeakQuestion, LexicalOnlyQuestion]).ShouldBeTrue();
        QueryVector(new(PositiveQuestion)).SequenceEqual(QueryVector(new(NegativeQuestion))).ShouldBeFalse();
    }
}
