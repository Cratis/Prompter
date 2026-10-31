// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Embeddings;

/// <summary>Shared Voyage input and request bounds, including conservative indexing framing.</summary>
public static class EmbeddingInputLimits
{
    /// <summary>The maximum number of inputs per request.</summary>
    public const int MaximumItems = 1000;

    /// <summary>The hard token ceiling for one input.</summary>
    public const int MaximumInputTokens = 32_000;

    /// <summary>The hard token ceiling for a complete request.</summary>
    public const int MaximumRequestTokens = 320_000;

    /// <summary>Bounds tokens by UTF-8 bytes plus input framing.</summary>
    /// <param name="input">One exact embedding input.</param>
    /// <returns>The conservative input token bound.</returns>
    public static long TokensFor(string input) => Encoding.UTF8.GetByteCount(input) + 64L;

    /// <summary>Honors a lower verified input ceiling without exceeding the provider limit.</summary>
    /// <param name="configured">The configured ceiling, or zero before price verification.</param>
    /// <returns>The effective per-input ceiling.</returns>
    public static int PerInput(int configured) => configured > 0 ? Math.Min(configured, MaximumInputTokens) : MaximumInputTokens;
}
