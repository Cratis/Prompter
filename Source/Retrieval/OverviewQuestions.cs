// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Retrieval;

/// <summary>Recognizes broad "what is Cratis good at / why use Cratis" questions that the docs overview pages answer.</summary>
public static partial class OverviewQuestions
{
    static readonly Regex _product = new(
        $@"\b({string.Join('|', ProductScope.Products.Where(product => product != InteractionProduct.OtherOrUnknown).Select(product => Regex.Escape(ProductScope.Display(product))))})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(RegexLimits.TimeoutMilliseconds));

    [GeneratedRegex(@"\bwhat\s+(is|'s|’s)\s+cratis\b\s*([?.,!]|$|(all\s+)?(about|for|good|great|best|strong)\b|in\s+(a\s+)?(nutshell|short|brief)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Defining { get; }

    [GeneratedRegex(@"\bcratis\b.{0,20}\b(good|great|best|strong)\s+(at|for)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex GoodAt { get; }

    [GeneratedRegex(@"\bcratis(['’]s?)?\s+((main|key|biggest)\s+)?(strengths?|benefits?|advantages?|selling\s+points?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Strengths { get; }

    [GeneratedRegex(@"\b(strengths?|benefits?|advantages?|pros)\s+of\s+(using\s+)?cratis\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex StrengthsOf { get; }

    [GeneratedRegex(@"\bwhy\s+((should|would|do|does)\s+)?((i|we|you|people|teams|developers)\s+)?(use|choose|pick|adopt|consider)\s+cratis\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Why { get; }

    /// <summary>Checks whether a question asks broadly about Cratis rather than about one product or task.</summary>
    /// <param name="question">The question text.</param>
    /// <returns>True for a broad overview question; false otherwise, including when matching times out.</returns>
    public static bool Matches(string question)
    {
        try
        {
            var broad = Defining.IsMatch(question) || GoodAt.IsMatch(question) || Strengths.IsMatch(question) ||
                StrengthsOf.IsMatch(question) || Why.IsMatch(question);
            return broad && !_product.IsMatch(question);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
