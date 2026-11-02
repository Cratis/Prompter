// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_the_text_overflows_the_bound : Specification
{
    IReadOnlyList<string> _messages = [];

    void Because()
    {
        var options = new PrompterOptions();
        var text = PublishedMarkdown.Neutralize("discord.gg/abc\n\n" + new string('a', PublishedMarkdown.MaximumLength + 1), options);
        _messages = DiscordAnswers.Split(Answer.Refusal(0.9, []) with { Text = text, IsRefusal = false }, options);
    }

    [Fact] void should_leave_no_scheme_less_invite_in_any_message() => _messages.Any(message => message.Contains("discord.gg/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
}
