// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Digest;

/// <summary>
/// Existing interaction metadata only; never question or answer text.
/// </summary>
/// <param name="Source">The interaction source.</param>
/// <param name="CitedPages">The cited documentation URLs.</param>
/// <param name="WasRefusal">Whether the answer was refused.</param>
/// <param name="Feedback">The optional feedback verdict.</param>
/// <param name="Category">The optional Lane A category.</param>
/// <param name="Product">The optional Lane A product key.</param>
public record DocsGapRow(string Source, IReadOnlyList<string> CitedPages, bool WasRefusal, string? Feedback, string? Category, string? Product);
