// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Charges the composite embedding length before allocating it for hashing or emission. A pathological page
/// stops at its affordable prefix rather than amplifying metadata without bound.
/// </summary>
/// <param name="remaining">The page-wide allowance in UTF-16 characters.</param>
internal sealed class EmbeddingOutputBudget(long remaining)
{
    long _remaining = remaining;

    /// <summary>Gets a value indicating whether a piece exceeded the remaining allowance.</summary>
    internal bool Exhausted { get; private set; }

    /// <summary>Charges a piece's embedding length without allocating the composite text.</summary>
    /// <param name="text">The embedding's constituent text.</param>
    /// <returns>Whether the piece fits.</returns>
    internal bool TryTake(EmbeddingText text)
    {
        // Keep this length in sync with Chunk.EmbeddingInputFor: "title — path\n\ncontent".
        var length = (long)text.Title.Length + text.HeadingPath.Length + text.Content.Length + 5;
        if (length > _remaining)
        {
            Exhausted = true;
            return false;
        }

        _remaining -= length;
        return true;
    }

    /// <summary>The text used to calculate one embedding's length.</summary>
    /// <param name="Title">The page title.</param>
    /// <param name="HeadingPath">The section's heading path.</param>
    /// <param name="Content">The piece's content.</param>
    internal readonly record struct EmbeddingText(string Title, string HeadingPath, string Content);
}
