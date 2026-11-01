// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Splits one chunk part into row groups for large tables and line-bounded pieces for large blocks, so that
/// parts without either come back unchanged and keep their id and hash.
/// </summary>
internal static partial class ChunkPieces
{
    const int MaxRepeatedHeaderLength = 500;

    [GeneratedRegex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TableSeparator { get; }

    /// <summary>
    /// Splits a trimmed part into pieces of at most <paramref name="maxChunkLength"/> characters.
    /// </summary>
    /// <param name="part">The trimmed part produced by the paragraph splitter.</param>
    /// <param name="maxChunkLength">The maximum length of a piece in characters.</param>
    /// <param name="maxTableLength">The length above which a table is split into row groups.</param>
    /// <param name="fence">The fenced-code state carried across the parts of a section; updated by this call.</param>
    /// <returns>The pieces in document order; the part itself when nothing needs splitting.</returns>
    public static IEnumerable<string> Split(string part, int maxChunkLength, int maxTableLength, FenceState fence)
    {
        var tableLimit = new PieceLength(Math.Min(maxTableLength, maxChunkLength));
        var chunkLimit = new PieceLength(maxChunkLength);
        var segments = Scan(part.Split('\n'), tableLimit, fence);
        if (part.Length <= maxChunkLength && segments.TrueForAll(segment => !segment.IsTable))
        {
            return [part];
        }

        return segments.SelectMany(segment => segment.IsTable ? RowGroups(segment.Lines, tableLimit, chunkLimit) : Lines(segment.Lines, chunkLimit));
    }

    static List<Segment> Scan(string[] lines, PieceLength tableLimit, FenceState fence)
    {
        var segments = new List<Segment>();
        var text = new List<string>();
        var index = 0;

        while (index < lines.Length)
        {
            fence.Observe(lines[index]);
            var run = fence.Open ? 0 : TableRunLength(lines.AsSpan(index));
            if (run > 0 && LengthOf(lines.AsSpan(index, run)) > tableLimit.Value)
            {
                if (text.Count > 0)
                {
                    segments.Add(new(text, false));
                    text = [];
                }

                segments.Add(new(lines[index..(index + run)], true));
                index += run;
                continue;
            }

            var take = Math.Max(run, 1);
            text.AddRange(lines[index..(index + take)]);
            index += take;
        }

        if (text.Count > 0)
        {
            segments.Add(new(text, false));
        }

        return segments;
    }

    static int TableRunLength(ReadOnlySpan<string> lines)
    {
        if (!HasTableHeader(lines))
        {
            return 0;
        }

        var end = 2;
        while (end < lines.Length && IsRow(new(lines[end])))
        {
            end++;
        }

        return end;
    }

    static bool HasTableHeader(ReadOnlySpan<string> lines) =>
        lines.Length > 1 && IsRow(new(lines[0])) && TableSeparator.IsMatch(lines[1]);

    static bool IsRow(MarkdownLine line) => line.Text.TrimStart().StartsWith('|');

    static int LengthOf(ReadOnlySpan<string> lines)
    {
        var length = lines.Length - 1;
        foreach (var line in lines)
        {
            length += line.Length;
        }

        return length;
    }

    static IEnumerable<string> RowGroups(IReadOnlyList<string> table, PieceLength tableLimit, PieceLength maxChunkLength)
    {
        var header = $"{table[0]}\n{table[1]}";
        if (header.Length > Math.Min(MaxRepeatedHeaderLength, maxChunkLength.Value / 4))
        {
            // An unbounded header repeated per group would amplify the output quadratically, so it is not repeated.
            return Lines(table, maxChunkLength);
        }

        return Group(table, new RowGrouper(new(header), tableLimit, maxChunkLength, table.Sum(line => line.Length + 1)));
    }

    static IEnumerable<string> Group(IReadOnlyList<string> table, RowGrouper grouper)
    {
        for (var index = 2; index < table.Count; index++)
        {
            foreach (var piece in grouper.Add(new(table[index])))
            {
                yield return piece;
            }
        }

        if (grouper.Finish() is { } last)
        {
            yield return last;
        }
    }

    static IEnumerable<string> Lines(IReadOnlyList<string> lines, PieceLength maxChunkLength)
    {
        var current = new StringBuilder();

        foreach (var line in lines)
        {
            if (current.Length > 0 && current.Length + 1 + line.Length > maxChunkLength.Value)
            {
                yield return current.ToString();
                current.Clear();
            }

            foreach (var slice in AppendOrSlice(current, new(line), maxChunkLength))
            {
                yield return slice;
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    static IEnumerable<string> AppendOrSlice(StringBuilder current, MarkdownLine line, PieceLength maxChunkLength)
    {
        if (line.Text.Length > maxChunkLength.Value)
        {
            return Slices(line, maxChunkLength);
        }

        current.Append(current.Length > 0 ? "\n" : string.Empty).Append(line.Text);
        return [];
    }

    static IEnumerable<string> Slices(MarkdownLine line, PieceLength maxChunkLength)
    {
        var start = 0;
        while (start < line.Text.Length)
        {
            var end = SliceEnd(line, start, maxChunkLength);
            yield return line.Text[start..end];
            start = end;
        }
    }

    static int SliceEnd(MarkdownLine line, int start, PieceLength room)
    {
        var end = Math.Min(start + room.Value, line.Text.Length);
        return end < line.Text.Length && end - start > 1 && char.IsHighSurrogate(line.Text[end - 1]) ? end - 1 : end;
    }

    readonly record struct MarkdownLine(string Text);

    readonly record struct PieceLength(int Value);

    /// <summary>
    /// Packs table rows into groups that repeat the header, spending a repeat budget so the total output stays
    /// within a constant factor of the table's own length.
    /// </summary>
    /// <param name="header">The header and separator to repeat within the allowance.</param>
    /// <param name="tableLimit">The preferred row group size.</param>
    /// <param name="maxChunkLength">The maximum fragment size, including its header.</param>
    /// <param name="repeatBudget">The table's input length, which bounds the extra repeated bytes.</param>
    sealed class RowGrouper(MarkdownLine header, PieceLength tableLimit, PieceLength maxChunkLength, int repeatBudget)
    {
        readonly StringBuilder _group = new();
        int _repeatBytes = repeatBudget;
        int _repeatChunks = (repeatBudget / Math.Max(1, tableLimit.Value)) + 1;
        bool _started;
        int _rows;

        public IEnumerable<string> Add(MarkdownLine row)
        {
            if (_rows > 0 && _group.Length + 1 + row.Text.Length > tableLimit.Value)
            {
                yield return Flush();
            }

            if (_group.Length == 0)
            {
                Begin();
            }

            if (NeedsFragments(row))
            {
                foreach (var fragment in Fragments(row))
                {
                    yield return fragment;
                }

                yield break;
            }

            _group.Append(_group.Length > 0 ? "\n" : string.Empty).Append(row.Text);
            _rows++;
        }

        public string? Finish() => _rows > 0 ? Flush() : null;

        bool NeedsFragments(MarkdownLine row) =>
            _rows == 0 && _group.Length + (_group.Length > 0 ? 1 : 0) + row.Text.Length > maxChunkLength.Value;

        string Flush()
        {
            var text = _group.ToString();
            _group.Clear();
            _rows = 0;
            return text;
        }

        void Begin()
        {
            if (_started)
            {
                _group.Append(NextPrefix());
                return;
            }

            _started = true;
            _group.Append(header.Text);
        }

        string NextPrefix()
        {
            // Bound both repeated bytes and repeated chunks while enumerating. Once either allowance is spent,
            // later fragments contain only original row text; no input is dropped.
            if (_repeatBytes < header.Text.Length + 1 || _repeatChunks == 0)
            {
                return string.Empty;
            }

            _repeatBytes -= header.Text.Length + 1;
            _repeatChunks--;
            return header.Text;
        }

        IEnumerable<string> Fragments(MarkdownLine row)
        {
            var prefix = _group.ToString();
            _group.Clear();
            var start = 0;
            while (start < row.Text.Length)
            {
                var joiner = prefix.Length > 0 ? 1 : 0;
                var end = SliceEnd(row, start, new(maxChunkLength.Value - prefix.Length - joiner));
                yield return joiner == 1 ? $"{prefix}\n{row.Text[start..end]}" : row.Text[start..end];
                start = end;
                prefix = NextPrefix();
            }
        }
    }

    sealed record Segment(IReadOnlyList<string> Lines, bool IsTable);
}
