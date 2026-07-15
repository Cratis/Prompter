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

        var paragraphs = new List<string>();
        foreach (var part in normalized.Split(ParagraphSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length <= MaxMessageLength)
            {
                paragraphs.Add(part);
            }
            else
            {
                paragraphs.AddRange(HardSplit(part));
            }
        }

        return paragraphs;
    }

    static IEnumerable<string> HardSplit(string paragraph)
    {
        for (var start = 0; start < paragraph.Length; start += MaxMessageLength)
        {
            yield return paragraph.Substring(start, Math.Min(MaxMessageLength, paragraph.Length - start));
        }
    }

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
