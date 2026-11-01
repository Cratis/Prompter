// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>Extracts prose, metadata and heading structure from bounded static site HTML.</summary>
public static partial class HtmlText
{
    [GeneratedRegex("<!--.*?-->|<(?<element>script|style|svg|noscript|template|iframe|form|nav|header|footer)\\b[^>]*>.*?</\\k<element>\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex NonContent { get; }

    [GeneratedRegex("<main\\b[^>]*>(?<body>.*?)</main\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex Main { get; }

    [GeneratedRegex("<body\\b[^>]*>(?<body>.*?)</body\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex Body { get; }

    [GeneratedRegex("<title\\b[^>]*>(?<text>.*?)</title\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex Title { get; }

    [GeneratedRegex("<meta\\b[^>]*>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Meta { get; }

    [GeneratedRegex("(?<name>[a-z]+)\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)')", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Attributes { get; }

    [GeneratedRegex("<h(?<level>[1-6])\\b[^>]*>(?<body>.*?)</h[1-6]\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex Headings { get; }

    [GeneratedRegex("<li\\b[^>]*>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex ListItems { get; }

    [GeneratedRegex("<br\\s*/?>|</(?:h[1-6]|p|div|section|article|ul|ol|table|tr|blockquote|pre|figure|figcaption|dd|dt)\\s*>", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Blocks { get; }

    [GeneratedRegex("<[^>]*>", RegexOptions.Singleline, 1000)]
    private static partial Regex Tags { get; }

    [GeneratedRegex("[^\\S\\n]+", RegexOptions.None, 1000)]
    private static partial Regex Spaces { get; }

    [GeneratedRegex("\\n{3,}", RegexOptions.None, 1000)]
    private static partial Regex BlankLines { get; }

    [GeneratedRegex("\\s+", RegexOptions.None, 1000)]
    private static partial Regex Whitespace { get; }

    /// <summary>Removes non-content markup and produces chunkable Markdown.</summary>
    /// <param name="html">The bounded HTML page.</param>
    /// <returns>Visible text with safe frontmatter and Markdown headings.</returns>
    /// <exception cref="IncompleteCrawl">The HTML exceeds the parsing time limit.</exception>
    public static string ToMarkdown(string html)
    {
        try
        {
            var clean = NonContent.Replace(html, string.Empty);
            var title = Scalar(Tags.Replace(Title.Match(clean).Groups["text"].Value, string.Empty), 200);
            var description = Description(clean);
            var main = Main.Match(clean);
            var body = main.Success ? main : Body.Match(clean);
            var text = body.Success ? body.Groups["body"].Value : clean;
            text = Headings.Replace(text, match => $"\n\n{new string('#', int.Parse(match.Groups["level"].Value, System.Globalization.CultureInfo.InvariantCulture))} {Tags.Replace(match.Groups["body"].Value, string.Empty)}\n\n");
            text = ListItems.Replace(text, "\n- ");
            text = Blocks.Replace(text, "\n\n");
            text = Tags.Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = string.Join('\n', Spaces.Replace(text, " ").Split('\n').Select(line => line.Trim()));
            text = BlankLines.Replace(text, "\n\n").Trim();
            if (title.Length > 0 && !text.Split('\n').Any(line => line.StartsWith("# ", StringComparison.Ordinal))) text = $"# {title}\n\n{text}";
            return Frontmatter(title, description) + text;
        }
        catch (RegexMatchTimeoutException)
        {
            throw new IncompleteCrawl();
        }
    }

    /// <summary>Creates bounded, single-line quoted metadata.</summary>
    /// <param name="value">The raw value.</param>
    /// <param name="maxCharacters">The character cap.</param>
    /// <returns>The normalized value.</returns>
    internal static string Scalar(string value, int maxCharacters)
    {
        var single = Whitespace.Replace(WebUtility.HtmlDecode(value), " ").Trim().Replace('"', '\'');
        return single[..Math.Min(single.Length, maxCharacters)];
    }

    /// <summary>Wraps available normalized metadata in frontmatter.</summary>
    /// <param name="title">The normalized title.</param>
    /// <param name="description">The normalized description.</param>
    /// <returns>Frontmatter, or empty if neither field is available.</returns>
    internal static string Frontmatter(string title, string description)
    {
        if (title.Length == 0 && description.Length == 0) return string.Empty;
        return "---\n" + (title.Length > 0 ? $"title: \"{title}\"\n" : string.Empty) +
            (description.Length > 0 ? $"description: \"{description}\"\n" : string.Empty) + "---\n";
    }

    static string Description(string html)
    {
        foreach (Match meta in Meta.Matches(html))
        {
            var attributes = Attributes.Matches(meta.Value).GroupBy(match => match.Groups["name"].Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Groups["value"].Value, StringComparer.OrdinalIgnoreCase);
            if (attributes.GetValueOrDefault("name")?.Equals("description", StringComparison.OrdinalIgnoreCase) == true)
                return Scalar(attributes.GetValueOrDefault("content") ?? string.Empty, 1000);
        }
        return string.Empty;
    }
}
