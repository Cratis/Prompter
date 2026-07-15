// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_formatting;

public class and_answer_has_citations : Specification
{
    string _result = null!;

    void Because() => _result = DiscordAnswers.Format(new(
        "Append events through the event log.",
        [new PageUrl("https://cratis.io/chronicle/events.md")],
        0.5,
        IsRefusal: false));

    [Fact] void should_start_with_the_answer() => _result.StartsWith("Append events").ShouldBeTrue();
    [Fact] void should_link_sources_without_markdown_extension() => _result.ShouldContain("<https://cratis.io/chronicle/events>");
}
