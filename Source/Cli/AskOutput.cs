// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Cli;

/// <summary>
/// Renders an <see cref="Answer"/> for the terminal <c language="csharp">ask</c> command.
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
                ? $"Retrieved {passages.Count} passage{(passages.Count == 1 ? string.Empty : "s")}, leading semantic similarity {Score(answer.Confidence)}:"
                : "Retrieved 0 passages.");
            lines.Add(string.Empty);

            lines.AddRange(passages.Select((passage, index) =>
                $"  [{index + 1}] rrf {passage.Score.ToString("F5", CultureInfo.InvariantCulture)}, semantic similarity {Score(passage.SemanticSimilarity)}  {passage.Page.Value}{Heading(passage)}"));

            if (passages.Count > 0)
            {
                lines.Add(string.Empty);
            }
        }

        lines.Add(answer.Text);
        lines.AddRange(answer.Citations.Select(citation => $"  - {citation.Value}{Note(answer, citation)}"));

        return lines;
    }

    /// <summary>
    /// The process exit code for an answer - non-zero on a refusal so scripts and CI probes can detect it.
    /// </summary>
    /// <param name="answer">The answer to derive the exit code from.</param>
    /// <returns><c language="csharp">1</c> when the answer is a refusal, otherwise <c language="csharp">0</c>.</returns>
    public static int ExitCode(Answer answer) => answer.IsRefusal ? 1 : 0;

    static string Note(Answer answer, PageUrl page) =>
        answer.CitationProvenance.FirstOrDefault(provenance => provenance.Page == page) is { } found ? $" — {found.Note}" : string.Empty;

    static string Score(double score) => score.ToString("F3", CultureInfo.InvariantCulture);

    static string Heading(Passage passage) =>
        string.IsNullOrEmpty(passage.HeadingPath) ? string.Empty : $" — {passage.HeadingPath}";
}
