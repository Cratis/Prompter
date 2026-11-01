// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Discord;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_sparse_page_indexes_are_cited : given.a_grounded_answerer
{
    void Establish()
    {
        string[] names = ["a", "b", "a", "c", "d", "e"];
        Found([.. names.Select(name => Passage(0.8) with { Page = new($"https://cratis.io/{name}.md") })]);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "E [5], B [2].")));
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_rewrite_original_page_five_to_displayed_source_two() => result.Text.ShouldEqual("E [2], B [1].");
    [Fact] void should_map_displayed_source_one_to_original_page_two() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/b.md", "https://cratis.io/e.md"]);
    [Fact] void should_render_the_same_mapping_on_discord() => DiscordAnswers.Format(result).ShouldContain("Sources: [1] <https://cratis.io/b> (index date unknown) · [2] <https://cratis.io/e> (index date unknown)");
    [Fact] void should_render_the_same_mapping_in_the_terminal() => AskOutput.Lines(result, verbose: false).Skip(1).ShouldEqual(["  [1] https://cratis.io/b.md — index date unknown", "  [2] https://cratis.io/e.md — index date unknown"]);
    [Fact] void should_distinguish_verbose_passage_numbers_from_citation_numbers() => string.Join('\n', AskOutput.Lines(result, verbose: true)).ShouldContain("  passage 6: rrf");
}
