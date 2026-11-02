// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Protection;

/// <summary>
/// Leaves examples intact while making unapproved prose destinations and mentions inert.
/// </summary>
public static partial class PublishedMarkdown
{
    const int MaximumProseLength = 32768;
    static readonly SearchValues<char> _ambiguousProse = SearchValues.Create("[]<>\\`");

    [GeneratedRegex(
        @"(?<definition>^ {0,3}\[[^\[\]\r\n]+\]:[^\r\n]*)|(?<link>!?\[(?<label>[^\[\]\r\n]*)\]\((?<destination>[^()\r\n]*)\))|(?<reference>!?\[(?<referenceLabel>[^\[\]\r\n]*)\]\[[^\[\]\r\n]*\])|(?<citation>\[\d+\])|(?<angle><[^>\r\n]*>)|(?<url>[a-z][a-z0-9+.-]*:[^\s<>\[\]()""`]+|www\.[^\s<>\[\]()""`]+)|(?<mention>(?<![\w@])@[a-z0-9_][a-z0-9_.-]*(?:/[a-z0-9_.-]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
        1000)]
    private static partial Regex Tokens { get; }

    /// <summary>
    /// Applies the publication policy before a generated payload is frozen or displayed.
    /// </summary>
    /// <param name="markdown">Generated Markdown, including any code examples.</param>
    /// <param name="options">The existing citation source policy.</param>
    /// <returns>Markdown with only citable active links, or an inert representation on ambiguity.</returns>
    public static string Neutralize(string markdown, PrompterOptions options)
    {
        try
        {
            return new Scanner(markdown, options).Transform() ?? Inert(markdown);
        }
        catch (RegexMatchTimeoutException)
        {
            return Inert(markdown);
        }
    }

    static string Inert(ReadOnlySpan<char> text)
    {
        var fence = new string('`', Math.Max(3, LongestRun(text, '`') + 1));
        return $"{fence}\n{text}\n{fence}";
    }

    static string Code(ReadOnlySpan<char> text)
    {
        var delimiter = new string('`', LongestRun(text, '`') + 1);
        var padding = text.StartsWith("`", StringComparison.Ordinal) || text.EndsWith("`", StringComparison.Ordinal) ? " " : string.Empty;
        return $"{delimiter}{padding}{text}{padding}{delimiter}";
    }

    static int LongestRun(ReadOnlySpan<char> text, char marker)
    {
        var maximum = 0;
        var run = 0;
        foreach (var character in text)
        {
            run = character == marker ? run + 1 : 0;
            maximum = Math.Max(maximum, run);
        }
        return maximum;
    }

    readonly record struct CodeRun(int Start, char Marker, int Length);

    sealed class Scanner(string markdown, PrompterOptions options)
    {
        readonly StringBuilder _output = new();

        public string? Transform()
        {
            var proseStart = 0;
            for (var position = 0; position < markdown.Length; position++)
            {
                if (markdown[position] is not ('`' or '~'))
                {
                    continue;
                }
                var opening = RunAt(position);
                var end = CodeEnd(opening);
                if (end == position)
                {
                    position += opening.Length - 1;
                    continue;
                }
                if (!AppendBeforeCode(proseStart, position, end))
                {
                    return null;
                }
                _output.Append(markdown.AsSpan(position, end - position));
                proseStart = end;
                position = end - 1;
            }
            return AppendProse(markdown[proseStart..]) ? _output.ToString() : null;
        }

        static bool IsLineEnding(char value) => value is '\r' or '\n';

        bool AppendBeforeCode(int proseStart, int codeStart, int end) => end >= 0 && AppendProse(markdown[proseStart..codeStart]);

        int CodeEnd(CodeRun opening)
        {
            var (start, marker, length) = opening;
            if (length >= 3 && AtLineStart(opening))
            {
                return FenceEnd(opening);
            }
            if (marker == '~')
            {
                return start;
            }
            return InlineEnd(opening);
        }

        int InlineEnd(CodeRun opening)
        {
            var (start, marker, length) = opening;
            for (var position = start + length; position < markdown.Length; position++)
            {
                // A newline can begin another Markdown block; only single-line inline spans are unambiguous here.
                if (IsLineEnding(markdown[position])) return -1;
                if (markdown[position] != marker)
                {
                    continue;
                }
                var closing = RunAt(position);
                if (closing.Length == length)
                {
                    return position + closing.Length;
                }
                position += closing.Length - 1;
            }
            return -1;
        }

        int FenceEnd(CodeRun opening)
        {
            var (start, marker, length) = opening;
            var lineEnd = markdown.IndexOf('\n', start);
            var infoEnd = lineEnd < 0 ? markdown.Length : lineEnd;
            if (marker == '`' && markdown.AsSpan(start + length, infoEnd - start - length).Contains('`'))
            {
                return -1;
            }
            for (var position = infoEnd; position < markdown.Length; position++)
            {
                if (markdown[position] != marker) continue;
                var closing = RunAt(position);
                var end = ClosingFenceEnd(opening, closing);
                if (end >= 0) return end;
                position += closing.Length - 1;
            }
            return markdown.Length;
        }

        int ClosingFenceEnd(CodeRun opening, CodeRun closing)
        {
            if (!AtLineStart(closing)) return -1;
            var position = closing.Start;
            var end = markdown.IndexOf('\n', position);
            end = end < 0 ? markdown.Length : end;
            return closing.Length >= opening.Length && markdown.AsSpan(position + closing.Length, end - position - closing.Length).Trim().IsEmpty ? end : -1;
        }

        CodeRun RunAt(int start)
        {
            var marker = markdown[start];
            var end = start;
            while (end < markdown.Length && markdown[end] == marker)
            {
                end++;
            }
            return new(start, marker, end - start);
        }

        bool AtLineStart(CodeRun run)
        {
            var position = run.Start;
            var lineStart = markdown.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
            var prefix = markdown.AsSpan(lineStart, position - lineStart);
            return prefix.Length <= 3 && prefix.Trim(' ').IsEmpty;
        }

        bool AppendProse(string prose)
        {
            if (prose.Length > MaximumProseLength)
            {
                return false;
            }
            var position = 0;
            foreach (Match token in Tokens.Matches(prose))
            {
                if (!AppendPlain(prose.AsSpan(position, token.Index - position)))
                {
                    return false;
                }
                _output.Append(TransformToken(token));
                position = token.Index + token.Length;
            }
            return AppendPlain(prose.AsSpan(position));
        }

        bool AppendPlain(ReadOnlySpan<char> text)
        {
            if (text.IndexOfAny(_ambiguousProse) >= 0 || text.Contains("&#", StringComparison.Ordinal))
            {
                return false;
            }
            _output.Append(text);
            return true;
        }

        string TransformToken(Match token)
        {
            if (token.Groups["link"].Success)
            {
                return Link(token);
            }
            if (token.Groups["reference"].Success)
            {
                return Label(token.Groups["referenceLabel"].Value);
            }
            if (token.Groups["angle"].Success)
            {
                return Citable(token.Value[1..^1]) ? token.Value : Code(WebUtility.HtmlEncode(token.Value));
            }
            if (token.Groups["url"].Success)
            {
                return Url(token);
            }
            return token.Groups["citation"].Success ? token.Value : Code(token.Value);
        }

        string Link(Match token)
        {
            var destination = token.Groups["destination"].Value;
            var originalLabel = token.Groups["label"].Value;
            var label = Label(originalLabel);
            if (!Citable(destination)) return $"{label} {Code(destination)}";
            if (label != originalLabel) return $"{label} {destination}";
            return token.Value;
        }

        string Url(Match token)
        {
            var url = token.Value.TrimEnd('.', ',', ';', ':', '!', '?');
            return (Citable(url) ? url : Code(url)) + token.Value[url.Length..];
        }

        string Label(string text) => text.IndexOfAny(['&', '\\']) >= 0 ? Code(text) : Tokens.Replace(text, match => Code(match.Value));

        bool Citable(string destination) =>
            destination.IndexOfAny(['%', '\\', '&', '`']) < 0 && !destination.Any(char.IsWhiteSpace) &&
            SourceHosts.IsCitable(new(destination), options);
    }
}
