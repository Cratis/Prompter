// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Answering;

/// <summary>Detects questions whose evidence is a near-tie between products, and renders and reads back the clarifying question.</summary>
public static partial class Clarifications
{
    const int ExaminedPassages = 4;
    const int MaximumOptions = 4;
    const string Quote = "> ";

    [GeneratedRegex(@"\b(chronicle|arc|fundamentals|components|cli)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex ProductName { get; }

    /// <summary>Detects an ambiguity between products.</summary>
    /// <param name="question">The question, including any scope and conversation.</param>
    /// <param name="qualified">The passages that met the similarity threshold, best first.</param>
    /// <param name="docsSiteUrl">The documentation site base URL.</param>
    /// <param name="margin">How close the runner-up product must be to the best one.</param>
    /// <returns>The clarification, or <see langword="null"/> when the question is not ambiguous.</returns>
    public static Clarification? Detect(Question question, IReadOnlyList<Passage> qualified, string docsSiteUrl, double margin)
    {
        if (question.Scope is not null || NamesProduct(question))
        {
            return null;
        }

        var best = qualified
            .OrderByDescending(passage => passage.SemanticSimilarity)
            .Take(ExaminedPassages)
            .Select(passage => (Product: ProductScope.ProductOf(passage.Page.Value, docsSiteUrl), passage.SemanticSimilarity))
            .Where(entry => entry.Product is not null)
            .GroupBy(entry => entry.Product!.Value)
            .Select(group => (Product: group.Key, Similarity: group.Max(entry => entry.SemanticSimilarity)))
            .OrderByDescending(entry => entry.Similarity)
            .ToArray();

        return best.Length >= 2 && best[1].Similarity >= best[0].Similarity - margin
            ? new([.. best.Take(MaximumOptions).Select(entry => entry.Product)])
            : null;
    }

    /// <summary>Renders the clarifying question with the original question quoted beneath it.</summary>
    /// <param name="clarification">The clarification.</param>
    /// <param name="question">The question that was asked.</param>
    /// <returns>The message text.</returns>
    public static string Text(Clarification clarification, string question)
    {
        var names = clarification.Options.Select(ProductScope.Display).ToArray();
        var list = names.Length == 2 ? $"{names[0]} or {names[1]}" : $"{string.Join(", ", names[..^1])} or {names[^1]}";
        var text = new StringBuilder($"This could be about {list}. Which product do you mean?\n");
        foreach (var line in question.Split('\n'))
        {
            text.Append('\n').Append(Quote).Append(line.TrimEnd('\r'));
        }

        return text.ToString();
    }

    /// <summary>Reads the original question back from a clarification message.</summary>
    /// <param name="content">The message content.</param>
    /// <returns>The question, or <see langword="null"/> when the message quotes none.</returns>
    public static string? QuestionFrom(string content)
    {
        var lines = content.Split('\n').Where(line => line.StartsWith(Quote, StringComparison.Ordinal)).Select(line => line[Quote.Length..]).ToArray();
        return lines.Length == 0 ? null : string.Join('\n', lines);
    }

    static bool NamesProduct(Question question) =>
        ProductName.IsMatch(question.Value) || question.Conversation.Messages.Any(message => ProductName.IsMatch(message.Content));
}
