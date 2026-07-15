// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Formats answers for Discord - citations as links, capped to Discord's message length.
/// </summary>
public static class DiscordAnswers
{
    const int MaxMessageLength = 2000;

    /// <summary>
    /// Formats an answer as a Discord message.
    /// </summary>
    /// <param name="answer">The answer to format.</param>
    /// <returns>The formatted message content.</returns>
    public static string Format(Answer answer)
    {
        var message = new StringBuilder(answer.Text.Trim());

        var citations = answer.Citations.ToArray();
        if (citations.Length > 0)
        {
            message
                .AppendLine()
                .AppendLine()
                .Append("Sources: ")
                .AppendJoin(" · ", citations.Select(page => $"<{page.Value.Replace(".md", string.Empty, StringComparison.OrdinalIgnoreCase)}>"));
        }

        var content = message.ToString();

        return content.Length <= MaxMessageLength ? content : $"{content[..(MaxMessageLength - 1)]}…";
    }
}
