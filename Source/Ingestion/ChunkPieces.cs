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
    [GeneratedRegex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TableSeparator { get; }

    /// <summary>
    /// Splits a trimmed part into pieces of at most <paramref name="maxChunkLength"/> characters.
    /// </summary>
    /// <param name="part">The trimmed part produced by the paragraph splitter.</param>
    /// <param name="maxChunkLength">The maximum length of a piece in characters.</param>
    /// <param name="maxTableLength">The length above which a table is split into row groups.</param>
    /// <returns>The pieces in document order; the part itself when nothing needs splitting.</returns>
    public static IEnumerable<string> Split(string part, int maxChunkLength, int maxTableLength)
    {
        var tableLimit = Math.Min(maxTableLength, maxChunkLength);
        var segments = Scan(part.Split('\n'), tableLimit);
        if (part.Length <= maxChunkLength && segments.TrueForAll(segment => !segment.IsTable))
        {
            return [part];
        }

        return segments.SelectMany(segment => segment.IsTable ? RowGroups(segment.Lines, tableLimit, maxChunkLength) : Lines(segment.Lines, maxChunkLength));
    }

    static List<Segment> Scan(string[] lines, int tableLimit)
    {
        var segments = new List<Segment>();
        var text = new List<string>();
        var inFence = false;
        var index = 0;

        while (index < lines.Length)
        {
            inFence ^= lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal);
            var run = inFence ? 0 : TableRunLength(lines, index);
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
        if (start + 1 >= lines.Length || !IsRow(lines[start]) || !TableSeparator.IsMatch(lines[start + 1]))
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

    static bool IsRow(string line) => line.TrimStart().StartsWith('|');

    static int LengthOf(string[] lines, int start, int count) => lines.Skip(start).Take(count).Sum(line => line.Length) + count - 1;

    static IEnumerable<string> RowGroups(IReadOnlyList<string> table, int tableLimit, int maxChunkLength)
    {
        var prefix = $"{table[0]}\n{table[1]}";
        var group = new StringBuilder(prefix);
        var rows = 0;

        foreach (var row in table.Skip(2))
        {
            if (rows > 0 && group.Length + 1 + row.Length > tableLimit)
            {
                foreach (var piece in Lines(group.ToString().Split('\n'), maxChunkLength))
                {
                    yield return piece;
                }

                group.Clear().Append(prefix);
                rows = 0;
            }

            group.Append('\n').Append(row);
            rows++;
        }

        foreach (var piece in Lines(group.ToString().Split('\n'), maxChunkLength))
        {
            yield return piece;
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

            if (line.Length > maxChunkLength)
            {
                foreach (var slice in Slices(line, maxChunkLength))
                {
                    yield return slice;
                }

                continue;
            }

            current.Append(current.Length > 0 ? "\n" : string.Empty).Append(line);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    static IEnumerable<string> Slices(string line, int maxChunkLength)
    {
        var start = 0;
        while (start < line.Length)
        {
            var end = Math.Min(start + maxChunkLength, line.Length);
            if (end < line.Length && end - start > 1 && char.IsHighSurrogate(line[end - 1]))
            {
                end--;
            }

            yield return line[start..end];
            start = end;
        }
    }

    sealed record Segment(IReadOnlyList<string> Lines, bool IsTable);
}
