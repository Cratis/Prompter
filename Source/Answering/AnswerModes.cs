// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Answering;

/// <summary>Classifies a question into the shape of answer it asks for, deterministically and without a model call.</summary>
public static partial class AnswerModes
{
    [GeneratedRegex(@"\b(which|what)\b.{0,60}\b(should i use|to use|do i need|is best|fits)\b|\bis there an? (tool|package|library|way)\b|\brecommend", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex Recommending { get; }

    [GeneratedRegex(@"\b(?<first>chronicle|arc|fundamentals|components|cli)\b\s+(?:vs\.?|versus|or)\s+(?<second>chronicle|arc|fundamentals|components|cli)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex Comparing { get; }

    /// <summary>Classifies a question.</summary>
    /// <param name="question">The question text.</param>
    /// <returns><see cref="AnswerMode.Recommendation"/> for "which tool should I use" style questions; otherwise <see cref="AnswerMode.Standard"/>.</returns>
    public static AnswerMode For(string question)
    {
        if (Recommending.IsMatch(question))
        {
            return AnswerMode.Recommendation;
        }

        var comparison = Comparing.Match(question);
        return comparison.Success && !string.Equals(comparison.Groups["first"].Value, comparison.Groups["second"].Value, StringComparison.OrdinalIgnoreCase)
            ? AnswerMode.Recommendation
            : AnswerMode.Standard;
    }
}
