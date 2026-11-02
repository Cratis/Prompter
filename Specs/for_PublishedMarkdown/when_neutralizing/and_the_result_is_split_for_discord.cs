// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_the_result_is_split_for_discord : Specification
{
    IReadOnlyList<string> _messages = [];

    void Because()
    {
        var text = PublishedMarkdown.Neutralize("`prefix\n\n```\n\n" + new string('a', 2100) + "\n\n[click](https://evil.invalid) @everyone\n\nsuffix`", new());
        _messages = DiscordAnswers.Split(Answer.Refusal(0.9, []) with { Text = text, IsRefusal = false }, new());
    }

    [Fact] void should_split_into_several_messages() => (_messages.Count > 1).ShouldBeTrue();
    [Fact] void should_leave_no_active_link_in_any_message() => _messages.Any(message => message.Contains("://evil", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_leave_no_raw_mention_in_any_message() => _messages.Any(message => message.Contains("@everyone", StringComparison.Ordinal)).ShouldBeFalse();
}
