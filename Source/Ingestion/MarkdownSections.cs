// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Ingestion;

/// <summary>Tracks fenced code and heading ancestry while collecting markdown sections.</summary>
/// <param name="title">The bounded page title.</param>
/// <param name="maximumTitleLength">The heading character limit.</param>
internal sealed class MarkdownSections(string title, int maximumTitleLength)
{
    readonly string _title = title;
    readonly List<Section> _sections = [];
    readonly StringBuilder _current = new();
    string _secondLevel = string.Empty;
    string _path = title;
    string _root = title;
    string? _heading;
    bool _inCodeFence;

    /// <summary>Splits lines at second and third level headings outside code fences.</summary>
    /// <param name="lines">The cleaned markdown lines.</param>
    /// <param name="title">The bounded page title.</param>
    /// <param name="maximumTitleLength">The heading character limit.</param>
    /// <returns>The collected sections.</returns>
    internal static List<Section> Split(string[] lines, string title, int maximumTitleLength)
    {
        var builder = new MarkdownSections(title, maximumTitleLength);
        foreach (var line in lines)
        {
            builder.Append(line);
        }

        builder.CloseSection();
        return builder._sections;
    }

    /// <summary>Bounds a heading without splitting a surrogate pair.</summary>
    /// <param name="heading">The heading text.</param>
    /// <param name="maximumTitleLength">The heading character limit.</param>
    /// <returns>The bounded heading.</returns>
    internal static string BoundHeading(ReadOnlySpan<char> heading, int maximumTitleLength)
    {
        var end = Math.Min(heading.Length, maximumTitleLength);
        if (end < heading.Length && char.IsHighSurrogate(heading[end - 1]))
        {
            end--;
        }

        return heading[..end].ToString();
    }

    void Append(string line)
    {
        if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
        {
            _inCodeFence = !_inCodeFence;
        }

        if (!_inCodeFence && TryHeading(line))
        {
            return;
        }

        _current.AppendLine(line.TrimEnd());
    }

    bool TryHeading(string line)
    {
        if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            CloseSection();
            _secondLevel = BoundHeading(line.AsSpan(3).Trim(), maximumTitleLength);
            _path = $"{_title} > {_secondLevel}";
            _root = _path;
            _heading = _secondLevel;
            return true;
        }

        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            CloseSection();
            var thirdLevel = BoundHeading(line.AsSpan(4).Trim(), maximumTitleLength);
            _path = _secondLevel.Length == 0
                ? $"{_title} > {thirdLevel}"
                : $"{_title} > {_secondLevel} > {thirdLevel}";
            _heading = thirdLevel;
            return true;
        }

        return line.StartsWith("# ", StringComparison.Ordinal);
    }

    void CloseSection()
    {
        if (_current.Length > 0)
        {
            _sections.Add(new(_path, _current.ToString(), _root, _heading));
            _current.Clear();
        }
    }
}
