// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Ingestion;

/// <summary>Removes MDX noise while preserving prose children and fenced code.</summary>
internal sealed class MdxContent
{
    readonly StringBuilder _result = new();
    bool _inCodeFence;
    bool _inComment;
    bool _inTag;

    /// <summary>Removes imports, expression comments and component tags outside backtick code fences.</summary>
    /// <param name="content">The markdown content to clean.</param>
    /// <returns>The cleaned markdown.</returns>
    internal static string Strip(string content)
    {
        var cleaner = new MdxContent();
        foreach (var line in content.Split('\n'))
        {
            cleaner.Append(line);
        }

        return cleaner._result.ToString();
    }

    static bool IsFence(string line) => line.TrimStart().StartsWith("```", StringComparison.Ordinal);

    static bool IsImport(string trimmed) =>
        trimmed.StartsWith("import ", StringComparison.Ordinal) && trimmed.Contains(" from ", StringComparison.Ordinal);

    static string? Continue(string line, string closing, ref bool active)
    {
        if (!active)
        {
            return line;
        }

        var close = line.IndexOf(closing, StringComparison.Ordinal);
        if (close < 0)
        {
            return null;
        }

        active = false;
        return line[(close + closing.Length)..];
    }

    /// <summary>Recognizes only block-level uppercase component tags, not lowercase HTML or inline generics.</summary>
    /// <param name="trimmed">The line with leading whitespace removed.</param>
    /// <returns>Whether the line starts with a component tag.</returns>
    static bool IsComponentTagStart(string trimmed)
    {
        if (trimmed.Length < 2 || trimmed[0] != '<')
        {
            return false;
        }

        var index = trimmed[1] == '/' ? 2 : 1;
        return index < trimmed.Length && char.IsAsciiLetterUpper(trimmed[index]);
    }

    void Append(string raw)
    {
        if (_inCodeFence)
        {
            _result.Append(raw).Append('\n');
            _inCodeFence = !IsFence(raw);
            return;
        }

        var line = Continue(raw, "*/}", ref _inComment);
        if (line is null)
        {
            return;
        }

        line = Continue(line, ">", ref _inTag);
        if (line is null)
        {
            return;
        }

        if (IsFence(line))
        {
            _result.Append(line).Append('\n');
            _inCodeFence = true;
            return;
        }

        AppendProse(RemoveInlineComments(line));
    }

    void AppendProse(string line)
    {
        var trimmed = line.TrimStart();
        if (IsImport(trimmed))
        {
            return;
        }

        if (IsComponentTagStart(trimmed))
        {
            _inTag = true;
            var rest = Continue(line, ">", ref _inTag);
            if (rest is null)
            {
                return;
            }

            line = rest;
        }

        _result.Append(line.Trim().Length == 0 ? string.Empty : line.TrimEnd()).Append('\n');
    }

    /// <summary>Removes complete inline comments; an unclosed comment truncates the line and continues on the next.</summary>
    /// <param name="line">The line to clean.</param>
    /// <returns>The line with comments removed.</returns>
    string RemoveInlineComments(string line)
    {
        var open = line.IndexOf("{/*", StringComparison.Ordinal);
        if (open < 0)
        {
            return line;
        }

        var result = new StringBuilder(line.Length);
        var position = 0;
        while (open >= 0)
        {
            result.Append(line, position, open - position);
            var close = line.IndexOf("*/}", open + 3, StringComparison.Ordinal);
            if (close < 0)
            {
                _inComment = true;
                return result.ToString();
            }

            position = close + 3;
            open = line.IndexOf("{/*", position, StringComparison.Ordinal);
        }

        return result.Append(line, position, line.Length - position).ToString();
    }
}
