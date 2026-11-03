// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Detects and removes retrieval meta-talk: a generated answer describing its own sources ("the excerpts
/// mention...", "this passage covers...") instead of speaking about Cratis directly, as the system prompt
/// requires. The prompt rule alone is not reliable, so this is enforced deterministically after generation.
/// </summary>
public static partial class RetrievalMetaTalk
{
    /// <summary>Gets the terms that mark retrieval meta-talk.</summary>
    public static readonly IReadOnlyList<string> Terms = ["excerpt", "passage", "the provided documentation"];

    [GeneratedRegex(@"[^.!?]*[.!?]+|[^.!?]+$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex SentenceBoundary { get; }

    /// <summary>Checks whether the text mentions a given term, treating hyphens and whitespace as equivalent.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="term">The term to find, ignoring case.</param>
    /// <returns><see langword="true"/> when the text mentions the term.</returns>
    public static bool Mentions(string text, string term) => Words(text).Contains(Words(term), StringComparison.OrdinalIgnoreCase);

    /// <summary>Checks whether the text mentions any retrieval meta-talk term.</summary>
    /// <param name="text">The text to search.</param>
    /// <returns><see langword="true"/> when the text mentions at least one of <see cref="Terms"/>.</returns>
    public static bool MentionsAny(string text) => Terms.Any(term => Mentions(text, term));

    /// <summary>
    /// Removes every sentence that mentions a meta-talk term, keeping a sentence that carries a citation marker
    /// even when it also mentions a term, and leaving the text unchanged when stripping would empty it entirely.
    /// </summary>
    /// <param name="text">The generated answer text.</param>
    /// <returns>The text with offending sentences removed.</returns>
    public static string Strip(string text)
    {
        if (!MentionsAny(text))
        {
            return text;
        }

        var lines = text.Split('\n').Select(StripLine).OfType<string>().ToArray();
        return lines.Length > 0 ? string.Join('\n', lines) : text;
    }

    static string? StripLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        var sentences = Sentences(line).ToArray();
        var kept = sentences.Where(sentence => !MentionsAny(sentence) || ProseCitations.Numbers(sentence).Any()).ToArray();
        if (kept.Length == sentences.Length)
        {
            return line;
        }

        var result = string.Concat(kept).Trim();
        return result.Length > 0 ? result : null;
    }

    static IEnumerable<string> Sentences(string line) =>
        SentenceBoundary.Matches(line).Select(match => match.Value).Where(sentence => sentence.Trim().Length > 0);

    static string Words(string text) => string.Join(' ', text.Split([' ', '-', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
