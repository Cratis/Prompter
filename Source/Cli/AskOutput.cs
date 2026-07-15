// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Cli;

/// <summary>
/// Renders an <see cref="Answer"/> for the terminal <c>ask</c> command.
/// </summary>
public static class AskOutput
{
    /// <summary>
    /// Renders the answer as the lines to print. In verbose mode the retrieved passages -
    /// score, page and heading, best first - are listed before the answer.
    /// </summary>
    /// <param name="answer">The answer to render.</param>
    /// <param name="verbose">Whether to list the retrieved passages before the answer.</param>
    /// <returns>The lines to write to the terminal, in order.</returns>
    public static IReadOnlyList<string> Lines(Answer answer, bool verbose)
    {
        var lines = new List<string>();

        if (verbose)
        {
            var passages = answer.Passages;
            lines.Add(passages.Count > 0
                ? $"Retrieved {passages.Count} passage{(passages.Count == 1 ? string.Empty : "s")}, top score {Score(passages[0].Score)}:"
                : "Retrieved 0 passages.");
            lines.Add(string.Empty);

            lines.AddRange(passages.Select((passage, index) =>
                $"  [{index + 1}] {Score(passage.Score)}  {passage.Page.Value}{Heading(passage)}"));

            if (passages.Count > 0)
            {
                lines.Add(string.Empty);
            }
        }

        lines.Add(answer.Text);
        lines.AddRange(answer.Citations.Select(citation => $"  - {citation.Value}"));

        return lines;
    }

    /// <summary>
    /// The process exit code for an answer - non-zero on a refusal so scripts and CI probes can detect it.
    /// </summary>
    /// <param name="answer">The answer to derive the exit code from.</param>
    /// <returns><c>1</c> when the answer is a refusal, otherwise <c>0</c>.</returns>
    public static int ExitCode(Answer answer) => answer.IsRefusal ? 1 : 0;

    static string Score(double score) => score.ToString("F3", CultureInfo.InvariantCulture);

    static string Heading(Passage passage) =>
        string.IsNullOrEmpty(passage.HeadingPath) ? string.Empty : $" — {passage.HeadingPath}";
}
