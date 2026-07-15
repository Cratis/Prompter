// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Cli;

/// <summary>
/// Represents the parsed arguments of the <c>ask</c> command.
/// </summary>
/// <param name="Question">The question to answer, with any flags removed.</param>
/// <param name="Verbose">Whether to print the retrieved passages before the answer.</param>
public record AskArguments(string Question, bool Verbose)
{
    /// <summary>
    /// Parses the tokens following the <c>ask</c> mode into an <see cref="AskArguments"/>. The
    /// <c>--verbose</c> (or <c>-v</c>) flag is recognized in any position; everything else is the question.
    /// </summary>
    /// <param name="tokens">The command-line tokens after the <c>ask</c> mode word.</param>
    /// <returns>The parsed <see cref="AskArguments"/>.</returns>
    public static AskArguments Parse(IEnumerable<string> tokens)
    {
        var all = tokens.ToArray();
        var verbose = all.Any(IsVerboseFlag);
        var question = string.Join(' ', all.Where(token => !IsVerboseFlag(token)));

        return new(question, verbose);
    }

    static bool IsVerboseFlag(string token) =>
        token.Equals("--verbose", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("-v", StringComparison.OrdinalIgnoreCase);
}
