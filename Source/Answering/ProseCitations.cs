// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Prompter.Answering;

/// <summary>Recognizes numeric prose citations without interpreting programming examples as references.</summary>
internal static partial class ProseCitations
{
    // Match protected text before citation tokens. Fences may close with a longer delimiter;
    // inline spans require the exact backtick run. An unfinished fence protects the remainder.
    // Attached numeric indexers, including chains, are protected even outside Markdown code.
    [GeneratedRegex("""
        ^[ ]{0,3}(?<backticks>`{3,})[^\r\n]*(?:\r?\n|\z)
            [\s\S]*?(?:^[ ]{0,3}\k<backticks>`*[ \t]*\r?$|\z)
        | ^[ ]{0,3}(?<tildes>~{3,})[^\r\n]*(?:\r?\n|\z)
            [\s\S]*?(?:^[ ]{0,3}\k<tildes>~*[ \t]*\r?$|\z)
        | \\[\s\S]
        | (?<inline>`+)(?!`)[\s\S]*?(?<!`)\k<inline>(?!`)
        | [\p{L}\p{N}_)\]}?!]\[[0-9]+\](?:\[[0-9]+\])*
        | \[(?<index>[0-9]+)\]
        """, RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace, 100)]
    private static partial Regex Tokens { get; }

    /// <summary>Gets prose reference numbers, using zero for an overflowed number.</summary>
    /// <param name="text">The generated Markdown.</param>
    /// <returns>Only reference numbers outside protected text.</returns>
    public static IEnumerable<int> Numbers(string text) => Tokens.Matches(text)
        .Where(match => match.Groups["index"].Success).Select(Number);

    /// <summary>Rewrites prose citations, leaving protected text byte-for-byte unchanged.</summary>
    /// <param name="text">The generated Markdown.</param>
    /// <param name="numbers">The original-to-displayed reference mapping.</param>
    /// <returns>Text with normalized prose references and invalid prose markers removed.</returns>
    public static string Remap(string text, IReadOnlyDictionary<int, int> numbers) => Tokens.Replace(text, match =>
    {
        if (!match.Groups["index"].Success)
        {
            return match.Value;
        }
        return numbers.TryGetValue(Number(match), out var number) ? $"[{number}]" : string.Empty;
    });

    static int Number(Match match) => int.TryParse(match.Groups["index"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;
}
