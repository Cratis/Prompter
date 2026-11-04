// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_RetrievalMetaTalk.when_stripping;

public class and_a_sentence_contains_dotted_names : Specification
{
    const string Text = "I couldn't find Vercel guidance in the Cratis documentation. The excerpts cover Next.js App Router on Node.js [1]. " +
        "Use app/.cratis/[...arc]/route.ts with Arc v1.2 [2].";
    string _result = null!;

    void Because() => _result = RetrievalMetaTalk.Strip(Text);

    [Fact] void should_remove_the_whole_meta_sentence() => _result.ShouldNotContain("js App Router");
    [Fact] void should_keep_the_other_sentences_intact() => _result.StartsWith("I couldn't find Vercel guidance in the Cratis documentation", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_dotted_names_in_kept_sentences() => _result.ShouldContain("Use app/.cratis/[...arc]/route.ts with Arc v1.2 [2]");
    [Fact] void should_carry_the_citation_over() => _result.ShouldContain("[1]");
}
