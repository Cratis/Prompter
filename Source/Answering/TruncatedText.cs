// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Recovers a safe answer from an answer text cut by the output cap. Only a prefix made of complete units is kept:
/// a unit ends with a line that ends a sentence or a citation, or with a closing code fence. An unfinished line,
/// sentence or code fence and everything after it is dropped. The prefix has to cite at least one passage; otherwise
/// nothing is recovered and the caller takes the honest fallback.
/// </summary>
static class TruncatedText
{
    /// <summary>The line that marks a shortened answer.</summary>
    public const string ShortenedMarker = "…";

    const string Fence = "```";

    /// <summary>The longest escape sequence a cut can split: a surrogate pair written as two <c language="text">\uXXXX</c> escapes.</summary>
    const int MaximumSplitEscape = 12;

    /// <summary>Recovers the safe prefix of a text string the output cap cut before its closing quote.</summary>
    /// <param name="envelope">The UTF-8 envelope.</param>
    /// <param name="start">The offset just after the property name of the text, or -1 when there is none.</param>
    /// <returns>The safe prefix followed by a shortened marker line, or <see langword="null"/> when nothing is safe.</returns>
    public static string? Salvage(byte[] envelope, int start) => Decode(envelope, start) is { } partial ? SafePrefix(partial) : null;

    /// <summary>Keeps complete units of a cut text, outside any unclosed code fence, when they cite a passage.</summary>
    /// <param name="partial">The decoded text up to the cut. Its last line is unfinished by construction.</param>
    /// <returns>The safe prefix followed by a shortened marker line, or <see langword="null"/> when nothing is safe.</returns>
    public static string? SafePrefix(string partial)
    {
        var lines = partial.Split('\n');
        var safeEnd = 0;
        var offset = 0;
        var insideFence = false;
        foreach (var line in lines.Take(lines.Length - 1))
        {
            offset += line.Length + 1;
            var trimmed = line.Trim();
            var isFence = trimmed.StartsWith(Fence, StringComparison.Ordinal);
            insideFence ^= isFence;
            safeEnd = ClosesUnit(insideFence, isFence, trimmed) ? offset : safeEnd;
        }

        var prefix = partial[..safeEnd].TrimEnd();
        return prefix.Length > 0 && ProseCitations.Numbers(prefix).Any() ? $"{prefix}\n{ShortenedMarker}" : null;
    }

    static bool ClosesUnit(bool insideFence, bool isFence, string line) => !insideFence && (isFence || EndsUnit(line));

    static bool EndsUnit(string line) => line.Length > 0 && line[^1] is '.' or '!' or '?' or ']';

    /// <summary>Decodes a JSON string that has no closing quote, dropping an escape sequence the cut split.</summary>
    /// <param name="envelope">The UTF-8 envelope.</param>
    /// <param name="start">The offset just after the property name, or -1 when there is none.</param>
    /// <returns>The decoded text, or <see langword="null"/> when no string starts there.</returns>
    static string? Decode(byte[] envelope, int start)
    {
        var quote = start < 0 ? -1 : Array.FindIndex(envelope, start, value => value is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or (byte)':'));
        if (quote < 0 || envelope[quote] != (byte)'"')
        {
            return null;
        }

        for (var end = envelope.Length; end > quote && envelope.Length - end <= MaximumSplitEscape; end--)
        {
            try
            {
                return JsonSerializer.Deserialize<string>((byte[])[.. envelope.AsSpan(quote, end - quote), (byte)'"']);
            }
            catch (JsonException)
            {
                // The cut split an escape sequence; retry without its last byte.
            }
        }

        return null;
    }
}
