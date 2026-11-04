// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ClientLanguages.when_excluding_variants;

public class and_go_is_named_mid_sentence : Specification
{
    [Theory]
    [InlineData("How do I append events in Go to Chronicle?")]
    [InlineData("can I build projections using Go with Arc")]
    [InlineData("is there a Go language client for Chronicle?")]
    [InlineData("I'm writing services in go and want to observe events")]
    void should_keep_the_go_pages(string question) =>
        ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question(question)), "https://www.cratis.io")
            .Any(prefix => prefix.Contains("/chronicle/go/", StringComparison.Ordinal)).ShouldBeFalse();

    [Theory]
    [InlineData("when do we go live with the event store?")]
    [InlineData("what should I do before go-live in production?")]
    [InlineData("let's go through how appending works")]
    void should_exclude_the_go_pages_for_the_verb(string question) =>
        ClientLanguages.ExcludedPrefixes(ClientLanguages.Named(new Question(question)), "https://www.cratis.io")
            .Any(prefix => prefix.Contains("/chronicle/go/", StringComparison.Ordinal)).ShouldBeTrue();
}
