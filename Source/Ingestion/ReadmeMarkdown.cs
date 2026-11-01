// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>Normalizes public READMEs without changing fenced code.</summary>
public static partial class ReadmeMarkdown
{
    [GeneratedRegex(@"<!--\s*(?<name>[\w-]+):start\s*-->.*?<!--\s*\k<name>:end\s*-->|<!--.*?-->", RegexOptions.IgnoreCase | RegexOptions.Singleline, 1000)]
    private static partial Regex Comments { get; }

    [GeneratedRegex(@"^\s*(?:!?\[!\[.*?\]\(.*?\)\]\(.*?\)|!\[.*?\]\(.*?\)|<a\b[^>]*>\s*<img\b[^>]*>\s*</a>|<img\b[^>]*>)\s*$", RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Badge { get; }

    [GeneratedRegex("<[^>]*>", RegexOptions.None, 1000)]
    private static partial Regex Tags { get; }

    [GeneratedRegex(@"(?<fence>^[ \t]*```[^\n]*\n.*?^[ \t]*```[ \t]*$|^[ \t]*~~~[^\n]*\n.*?^[ \t]*~~~[ \t]*$)", RegexOptions.Multiline | RegexOptions.Singleline, 1000)]
    private static partial Regex Fences { get; }

    /// <summary>Cleans volatile decoration and prepends safe overview metadata.</summary>
    /// <param name="markdown">The README body.</param>
    /// <param name="title">The repository title.</param>
    /// <param name="description">The public repository description.</param>
    /// <param name="maxChars">The retained body character limit.</param>
    /// <returns>The normalized README.</returns>
    /// <exception cref="IncompleteCrawl">The README exceeds the parsing time limit.</exception>
    public static string Clean(string markdown, string title, string description, int maxChars)
    {
        try
        {
            var lines = new List<string>();
            var fence = string.Empty;
            var sections = Fences.Split(markdown.Replace("\r\n", "\n", StringComparison.Ordinal));
            var cleaned = string.Concat(sections.Select((section, index) => index % 2 == 0 ? Comments.Replace(section, string.Empty) : section));
            foreach (var line in cleaned.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
                {
                    var marker = trimmed[..3];
                    if (fence.Length == 0) fence = marker;
                    else if (fence == marker) fence = string.Empty;
                    lines.Add(line);
                    continue;
                }
                if (fence.Length == 0 && Badge.IsMatch(line)) continue;
                var text = fence.Length == 0 ? Tags.Replace(line, string.Empty).Trim() : line;
                if (text.Length == 0 && (lines.Count == 0 || lines[^1].Length == 0)) continue;
                lines.Add(text);
            }
            var body = string.Join('\n', lines).Trim();
            if (body.Length > maxChars)
            {
                var end = body.LastIndexOf('\n', maxChars - 1);
                body = end < 0 ? string.Empty : body[..end].TrimEnd();
            }
            title = HtmlText.Scalar(title, 200);
            description = HtmlText.Scalar(description, 350);
            if (!body.Split('\n').Any(line => line.StartsWith("# ", StringComparison.Ordinal))) body = $"# {title}\n\n{body}";
            return (HtmlText.Frontmatter(title, description) + body).TrimEnd();
        }
        catch (RegexMatchTimeoutException)
        {
            throw new IncompleteCrawl();
        }
    }
}
