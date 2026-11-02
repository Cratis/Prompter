// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Reads the position of a chunk within its page from the chunk id ({page}#summary, {page}#{index} or {page}#{index}.{piece}).
/// </summary>
public static class ChunkOrder
{
    static readonly (int Index, int Piece) _unknown = (int.MaxValue, 0);

    /// <summary>
    /// Parses the order of a chunk.
    /// </summary>
    /// <param name="chunkId">The chunk id, when known.</param>
    /// <returns>The chunk index and piece; the summary is index -1, and an unknown or malformed id sorts last.</returns>
    public static (int Index, int Piece) Of(string? chunkId)
    {
        var separator = chunkId?.LastIndexOf('#') ?? -1;
        if (chunkId is null || separator < 0)
        {
            return _unknown;
        }

        var local = chunkId[(separator + 1)..];
        if (local == "summary")
        {
            return (-1, 0);
        }

        var parts = local.Split('.');
        var piece = 0;
        return parts.Length is 1 or 2 && Number(parts[0], out var index) && (parts.Length == 1 || Number(parts[1], out piece))
            ? (index, piece)
            : _unknown;
    }

    static bool Number(string text, out int value) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
