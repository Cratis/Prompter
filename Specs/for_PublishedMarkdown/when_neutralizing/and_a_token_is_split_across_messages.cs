// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_a_token_is_split_across_messages : Specification
{
    IReadOnlyList<string> _messages = [];

    void Because()
    {
        var text = "https://cratis.io/" + new string('a', 1982) + "https://evil.invalid";
        _messages = DiscordAnswers.Split(Answer.Refusal(0.9, []) with { Text = text, IsRefusal = false }, new());
    }

    [Fact] void should_leave_no_active_link_in_any_message() => _messages.Any(message => message.Contains("https://evil", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
    [Fact] void should_keep_every_message_idempotent() => _messages.All(message => Protection.PublishedMarkdown.Neutralize(message, new()) == message).ShouldBeTrue();
}
