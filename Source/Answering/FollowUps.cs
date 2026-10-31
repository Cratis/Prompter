// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Filters model-suggested follow-up questions down to short, plain, useful ones.</summary>
public static class FollowUps
{
    const int MaximumLength = 80;
    const int MaximumCount = 3;
    static readonly string[] _forbidden = ["\n", "\r", "://", "www.", "@", "<"];

    /// <summary>Keeps at most three distinct suggestions of 1-80 characters that hold no line break, link, mention or markup.</summary>
    /// <param name="raw">The suggestions the model returned.</param>
    /// <param name="question">The question that was asked; a suggestion equal to it is dropped.</param>
    /// <returns>The suggestions to offer.</returns>
    public static IReadOnlyList<string> From(IEnumerable<string> raw, string question) =>
        [.. raw
            .Select(suggestion => suggestion.Trim())
            .Where(suggestion => suggestion.Length is >= 1 and <= MaximumLength && !_forbidden.Any(suggestion.Contains) &&
                !string.Equals(suggestion, question.Trim(), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumCount)];
}
