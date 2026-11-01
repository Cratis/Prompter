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
        var tableLimit = Math.Min(maxTableLength, maxChunkLength);
        var segments = Scan(part.Split('\n'), tableLimit, fence);
        if (part.Length <= maxChunkLength && segments.TrueForAll(segment => !segment.IsTable))
        {
            return [part];
        }

        return segments.SelectMany(segment => segment.IsTable ? RowGroups(segment.Lines, tableLimit, maxChunkLength) : Lines(segment.Lines, maxChunkLength));
    }

    static List<Segment> Scan(string[] lines, int tableLimit, FenceState fence)
    {
        var segments = new List<Segment>();
        var text = new List<string>();
        var index = 0;

        while (index < lines.Length)
        {
            fence.Observe(lines[index]);
            var run = fence.Open ? 0 : TableRunLength(lines, index);
            if (run > 0 && LengthOf(lines, index, run) > tableLimit)
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

    static int TableRunLength(string[] lines, int start)
    {
        if (!HasTableHeader(lines, start))
        {
            return 0;
        }

        var end = start + 2;
        while (end < lines.Length && IsRow(lines[end]))
        {
            end++;
        }

        return end - start;
    }

    static bool HasTableHeader(string[] lines, int start) =>
        start + 1 < lines.Length && IsRow(lines[start]) && TableSeparator.IsMatch(lines[start + 1]);

    static bool IsRow(string line) => line.TrimStart().StartsWith('|');

    static int LengthOf(string[] lines, int start, int count)
    {
        var length = count - 1;
        for (var offset = 0; offset < count; offset++)
        {
            length += lines[start + offset].Length;
        }

        return length;
    }

    static IEnumerable<string> RowGroups(IReadOnlyList<string> table, int tableLimit, int maxChunkLength)
    {
        var header = $"{table[0]}\n{table[1]}";
        if (header.Length > Math.Min(MaxRepeatedHeaderLength, maxChunkLength / 4))
        {
            // An unbounded header repeated per group would amplify the output quadratically, so it is not repeated.
            return Lines(table, maxChunkLength);
        }

        return Group(table, new RowGrouper(header, tableLimit, maxChunkLength, table.Sum(line => line.Length + 1)));
    }

    static IEnumerable<string> Group(IReadOnlyList<string> table, RowGrouper grouper)
    {
        for (var index = 2; index < table.Count; index++)
        {
            foreach (var piece in grouper.Add(table[index]))
            {
                yield return piece;
            }
        }

        if (grouper.Finish() is { } last)
        {
            yield return last;
        }
    }

    static IEnumerable<string> Lines(IReadOnlyList<string> lines, int maxChunkLength)
    {
        var current = new StringBuilder();

        foreach (var line in lines)
        {
            if (current.Length > 0 && current.Length + 1 + line.Length > maxChunkLength)
            {
                yield return current.ToString();
                current.Clear();
            }

            foreach (var slice in AppendOrSlice(current, line, maxChunkLength))
            {
                yield return slice;
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    static IEnumerable<string> AppendOrSlice(StringBuilder current, string line, int maxChunkLength)
    {
        if (line.Length > maxChunkLength)
        {
            return Slices(line, maxChunkLength);
        }

        current.Append(current.Length > 0 ? "\n" : string.Empty).Append(line);
        return [];
    }

    static IEnumerable<string> Slices(string line, int maxChunkLength)
    {
        var start = 0;
        while (start < line.Length)
        {
            var end = SliceEnd(line, start, maxChunkLength);
            yield return line[start..end];
            start = end;
        }
    }

    static int SliceEnd(string line, int start, int room)
    {
        var end = Math.Min(start + room, line.Length);
        return end < line.Length && end - start > 1 && char.IsHighSurrogate(line[end - 1]) ? end - 1 : end;
    }

    /// <summary>
    /// Packs table rows into groups that repeat the header, spending a repeat budget so the total output stays
    /// within a constant factor of the table's own length.
    /// </summary>
    /// <param name="header">The header and separator to repeat within the allowance.</param>
    /// <param name="tableLimit">The preferred row group size.</param>
    /// <param name="maxChunkLength">The maximum fragment size, including its header.</param>
    /// <param name="repeatBudget">The table's input length, which bounds the extra repeated bytes.</param>
    sealed class RowGrouper(string header, int tableLimit, int maxChunkLength, int repeatBudget)
    {
        readonly StringBuilder _group = new();
        int _repeatBytes = repeatBudget;
        int _repeatChunks = (repeatBudget / Math.Max(1, tableLimit)) + 1;
        bool _started;
        int _rows;

        public IEnumerable<string> Add(string row)
        {
            if (_rows > 0 && _group.Length + 1 + row.Length > tableLimit)
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

            _group.Append(_group.Length > 0 ? "\n" : string.Empty).Append(row);
            _rows++;
        }

        public string? Finish() => _rows > 0 ? Flush() : null;

        bool NeedsFragments(string row) =>
            _rows == 0 && _group.Length + (_group.Length > 0 ? 1 : 0) + row.Length > maxChunkLength;

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
            _group.Append(header);
        }

        string NextPrefix()
        {
            // Bound both repeated bytes and repeated chunks while enumerating. Once either allowance is spent,
            // later fragments contain only original row text; no input is dropped.
            if (_repeatBytes < header.Length + 1 || _repeatChunks == 0)
            {
                return string.Empty;
            }

            _repeatBytes -= header.Length + 1;
            _repeatChunks--;
            return header;
        }

        IEnumerable<string> Fragments(string row)
        {
            var prefix = _group.ToString();
            _group.Clear();
            var start = 0;
            while (start < row.Length)
            {
                var joiner = prefix.Length > 0 ? 1 : 0;
                var end = SliceEnd(row, start, maxChunkLength - prefix.Length - joiner);
                yield return joiner == 1 ? $"{prefix}\n{row[start..end]}" : row[start..end];
                start = end;
                prefix = NextPrefix();
            }
        }
    }

    sealed record Segment(IReadOnlyList<string> Lines, bool IsTable);
}
