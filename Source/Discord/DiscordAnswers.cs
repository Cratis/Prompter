// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Formats answers for Discord - citations as links, within Discord's message length.
/// </summary>
public static class DiscordAnswers
{
    const int MaxMessageLength = 2000;
    const int MaxChunks = 3;
    const string ParagraphSeparator = "\n\n";
    const string Ellipsis = "…";
    const string CodeFence = "```";

    /// <summary>
    /// Formats an answer as a single Discord message, capped to Discord's message length.
    /// </summary>
    /// <param name="answer">The answer to format.</param>
    /// <returns>The formatted message content.</returns>
    /// <remarks>
    /// Kept for the <c>/ask</c> slash command, which still replies with a single message. Mentions use
    /// <see cref="Split(Answer)"/> to spread long answers across successive messages instead of truncating.
    /// </remarks>
    public static string Format(Answer answer)
    {
        var content = BuildContent(answer);
        return content.Length <= MaxMessageLength ? content : $"{content[..(MaxMessageLength - 1)]}{Ellipsis}";
    }

    /// <summary>
    /// Splits an answer into one to three successive Discord messages, each within Discord's message length.
    /// </summary>
    /// <param name="answer">The answer to split.</param>
    /// <returns>The message chunks to send in order - the "Sources" line always rides on the last chunk.</returns>
    /// <remarks>
    /// A short answer yields a single chunk identical to <see cref="Format(Answer)"/>. A longer answer is
    /// split on blank-line paragraph boundaries and packed greedily; a paragraph longer than the message
    /// limit is hard-split. Content is never spread across more than three messages - if it would overflow,
    /// the third chunk is truncated with an ellipsis so the sources still make it through.
    /// </remarks>
    public static IReadOnlyList<string> Split(Answer answer)
    {
        var content = BuildContent(answer);
        if (content.Length <= MaxMessageLength)
        {
            return [content];
        }

        var chunks = Pack(Paragraphs(answer.Text));
        return chunks.Count == 0 ? [Format(answer)] : AppendSources(chunks, SourcesLine(answer));
    }

    static string BuildContent(Answer answer)
    {
        var body = answer.Text.Trim();
        var sources = SourcesLine(answer);
        return sources.Length == 0 ? body : $"{body}{ParagraphSeparator}{sources}";
    }

    static string SourcesLine(Answer answer)
    {
        var citations = answer.Citations.ToArray();
        if (citations.Length == 0)
        {
            return string.Empty;
        }

        var links = citations.Select(page => $"<{page.Value.Replace(".md", string.Empty, StringComparison.OrdinalIgnoreCase)}>");
        return $"Sources: {string.Join(" · ", links)}";
    }

    static List<string> Paragraphs(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        // Segment on blank lines, but keep a fenced code block whole - blank lines inside a fence
        // are part of the block, and a code block is always its own paragraph so it stays atomic.
        var segments = new List<string>();
        var current = new StringBuilder();
        var inFence = false;
        foreach (var line in normalized.Split('\n'))
        {
            if (IsFenceDelimiter(line))
            {
                if (inFence)
                {
                    AppendLine(current, line);
                    Flush(segments, current);
                }
                else
                {
                    Flush(segments, current);
                    AppendLine(current, line);
                }

                inFence = !inFence;
            }
            else if (!inFence && line.Trim().Length == 0)
            {
                Flush(segments, current);
            }
            else
            {
                AppendLine(current, line);
            }
        }

        Flush(segments, current);

        var paragraphs = new List<string>();
        foreach (var segment in segments)
        {
            if (segment.Length <= MaxMessageLength)
            {
                paragraphs.Add(segment);
            }
            else
            {
                paragraphs.AddRange(HardSplit(segment));
            }
        }

        return paragraphs;
    }

    static void AppendLine(StringBuilder builder, string line)
    {
        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(line);
    }

    static void Flush(List<string> segments, StringBuilder builder)
    {
        var segment = builder.ToString().Trim();
        if (segment.Length > 0)
        {
            segments.Add(segment);
        }

        builder.Clear();
    }

    static bool IsFenceDelimiter(string line) => line.TrimStart().StartsWith(CodeFence, StringComparison.Ordinal);

    static IEnumerable<string> HardSplit(string paragraph) =>
        paragraph.StartsWith(CodeFence, StringComparison.Ordinal) ? HardSplitCodeBlock(paragraph) : HardSplitText(paragraph);

    static IEnumerable<string> HardSplitText(string text)
    {
        for (var start = 0; start < text.Length; start += MaxMessageLength)
        {
            yield return text.Substring(start, Math.Min(MaxMessageLength, text.Length - start));
        }
    }

    static IEnumerable<string> HardSplitCodeBlock(string paragraph)
    {
        var lines = paragraph.Split('\n');
        var open = $"{CodeFence}{LanguageOf(lines[0])}";
        var contentEnd = lines.Length > 1 && IsFenceDelimiter(lines[^1]) ? lines.Length - 1 : lines.Length;
        var budget = MaxMessageLength - open.Length - CodeFence.Length - 2;

        if (budget <= 0)
        {
            foreach (var piece in HardSplitText(paragraph))
            {
                yield return piece;
            }

            yield break;
        }

        // Pack whole code lines, keeping each outgoing message a self-contained fenced block that
        // closes with ``` and reopens with the same language hint on the next message.
        var current = new StringBuilder();
        for (var index = 1; index < contentEnd; index++)
        {
            foreach (var segment in WrapLine(lines[index], budget))
            {
                if (current.Length == 0)
                {
                    current.Append(segment);
                }
                else if (current.Length + 1 + segment.Length <= budget)
                {
                    current.Append('\n').Append(segment);
                }
                else
                {
                    yield return $"{open}\n{current}\n{CodeFence}";
                    current.Clear().Append(segment);
                }
            }
        }

        yield return $"{open}\n{current}\n{CodeFence}";
    }

    static IEnumerable<string> WrapLine(string line, int budget)
    {
        if (line.Length <= budget)
        {
            yield return line;
            yield break;
        }

        for (var start = 0; start < line.Length; start += budget)
        {
            yield return line.Substring(start, Math.Min(budget, line.Length - start));
        }
    }

    static string LanguageOf(string openLine) => openLine.Trim().TrimStart('`').Trim();

    static List<string> Pack(List<string> paragraphs)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            if (current.Length == 0)
            {
                current.Append(paragraph);
            }
            else if (current.Length + ParagraphSeparator.Length + paragraph.Length <= MaxMessageLength)
            {
                current.Append(ParagraphSeparator).Append(paragraph);
            }
            else
            {
                chunks.Add(current.ToString());
                current.Clear().Append(paragraph);
            }
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks;
    }

    static List<string> AppendSources(List<string> chunks, string sources)
    {
        if (chunks.Count <= MaxChunks)
        {
            if (sources.Length == 0)
            {
                return chunks;
            }

            var last = chunks[^1];
            if (last.Length + ParagraphSeparator.Length + sources.Length <= MaxMessageLength)
            {
                chunks[^1] = $"{last}{ParagraphSeparator}{sources}";
                return chunks;
            }

            if (chunks.Count < MaxChunks)
            {
                chunks.Add(sources);
                return chunks;
            }

            chunks[^1] = $"{Truncate(last, RoomForBody(sources))}{ParagraphSeparator}{sources}";
            return chunks;
        }

        var kept = chunks.Take(MaxChunks).ToList();
        kept[^1] = sources.Length == 0
            ? Truncate(kept[^1], MaxMessageLength)
            : $"{Truncate(kept[^1], RoomForBody(sources))}{ParagraphSeparator}{sources}";
        return kept;
    }

    static int RoomForBody(string sources) => MaxMessageLength - ParagraphSeparator.Length - sources.Length;

    static string Truncate(string text, int room)
    {
        if (room <= Ellipsis.Length)
        {
            return Ellipsis;
        }

        return text.Length + Ellipsis.Length <= room ? $"{text}{Ellipsis}" : $"{text[..(room - Ellipsis.Length)]}{Ellipsis}";
    }
}
