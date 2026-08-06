// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Composes the comment Prompter leaves on an issue it can answer.
/// </summary>
/// <remarks>
/// Two things every comment says. Where the answer came from, as links a reader can check — an unsourced
/// answer on a tracker is worth less than no answer. And that a bot wrote it, plainly, along with how to stop
/// it: a maintainer who disagrees should not have to work out who to tell.
/// </remarks>
public static class IssueAnswerComment
{
    /// <summary>
    /// Builds the comment body.
    /// </summary>
    /// <param name="answer">The grounded answer.</param>
    /// <param name="optOutLabel">The label that stops Prompter answering an issue.</param>
    /// <returns>The comment body.</returns>
    public static string For(Answer answer, string optOutLabel)
    {
        var citations = answer.Citations.ToArray();
        var sources = citations.Length > 0
            ? "\n\n**Sources**\n" + string.Join('\n', citations.Select(citation => $"- {citation}"))
            : string.Empty;

        const string attribution =
            "<sub>Answered by [Prompter](https://github.com/Cratis/Prompter) from the published documentation — " +
            "it can be wrong, and a maintainer's word beats mine. Label this issue `";

        return $"{answer.Text.Trim()}{sources}\n\n{attribution}{optOutLabel}` to stop me commenting on it.</sub>";
    }
}
